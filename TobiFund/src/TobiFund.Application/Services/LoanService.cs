using Microsoft.EntityFrameworkCore;
using TobiFund.Application.Common;
using TobiFund.Application.DTOs;
using TobiFund.Application.Interfaces;
using TobiFund.Domain.Entities;
using TobiFund.Domain.Enums;

namespace TobiFund.Application.Services;

/// <summary>ثبت وام، تولید خودکار اقساط و پرداخت اقساط</summary>
public class LoanService : ILoanService
{
    private readonly IApplicationDbContext _context;
    private readonly IAuditService _audit;
    private readonly ICurrentUserService _currentUser;
    private readonly ISettingsService _settings;
    private readonly IAppLogger _logger;

    public LoanService(
        IApplicationDbContext context,
        IAuditService audit,
        ICurrentUserService currentUser,
        ISettingsService settings,
        IAppLogger logger)
    {
        _context = context;
        _audit = audit;
        _currentUser = currentUser;
        _settings = settings;
        _logger = logger;
    }

    public async Task<ServiceResult<LoanDto>> CreateLoanAsync(CreateLoanDto dto)
    {
        var member = await _context.Members.FirstOrDefaultAsync(m => m.Id == dto.MemberId && !m.IsDeleted);
        if (member is null)
            return ServiceResult<LoanDto>.Fail("عضو یافت نشد.");
        if (member.Status != MemberStatus.Active)
            return ServiceResult<LoanDto>.Fail("عضو غیرفعال است.");
        if (dto.Amount <= 0)
            return ServiceResult<LoanDto>.Fail("مبلغ وام باید بزرگتر از صفر باشد.");
        if (dto.InstallmentCount <= 0)
            return ServiceResult<LoanDto>.Fail("تعداد اقساط باید بزرگتر از صفر باشد.");

        var defaultCount = await _settings.GetIntAsync(SettingKeys.DefaultInstallmentCount, 10);
        if (dto.InstallmentCount != defaultCount && !_currentUser.IsAdmin)
            return ServiceResult<LoanDto>.Fail("تنها مدیر می‌تواند تعداد اقساط پیش‌فرض را تغییر دهد.");

        var firstDays = await _settings.GetIntAsync(SettingKeys.FirstInstallmentDays, 30);
        if (firstDays < 0)
            return ServiceResult<LoanDto>.Fail("تعداد روز تا اولین سررسید نامعتبر است.");

        // مبلغ پایه هر قسط + باقیمانده روی قسط آخر
        var baseAmount = Math.Floor(dto.Amount / dto.InstallmentCount);
        var lastAmount = dto.Amount - baseAmount * (dto.InstallmentCount - 1);

        await using var transaction = await _context.BeginTransactionAsync();
        try
        {
            var loan = new Loan
            {
                MemberId = dto.MemberId,
                Amount = dto.Amount,
                LoanDate = dto.LoanDate.Date,
                InstallmentCount = dto.InstallmentCount,
                InstallmentAmount = baseAmount,
                Description = dto.Description?.Trim(),
                CreatedAt = DateTime.Now,
                CreatedBy = _currentUser.UserName,
                IsDeleted = false
            };
            _context.Loans.Add(loan);
            await _context.SaveChangesAsync();

            // تولید خودکار اقساط:
            // اولین سررسید = تاریخ وام + X روز (پیش‌فرض ۳۰ روز)
            // سررسیدهای بعدی به فاصله یک ماه شمسی
            var firstDueDate = loan.LoanDate.AddDays(firstDays);
            for (var i = 1; i <= dto.InstallmentCount; i++)
            {
                var dueDate = i == 1
                    ? firstDueDate
                    : PersianDateHelper.AddPersianMonths(firstDueDate, i - 1);
                var amount = i == dto.InstallmentCount ? lastAmount : baseAmount;
                _context.LoanInstallments.Add(new LoanInstallment
                {
                    LoanId = loan.Id,
                    InstallmentNumber = i,
                    DueDate = dueDate,
                    Amount = amount,
                    PaidAmount = 0,
                    Status = InstallmentStatus.Future,
                    CreatedAt = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();
            await _audit.LogAsync(AppConstants.AuditActions.LoanCreated, "Loan", loan.Id.ToString(),
                $"وام {dto.Amount:N0} ({dto.InstallmentCount} قسط) برای عضو {member.MemberCode} ({member.FullName}) ثبت شد.");
            await transaction.CommitAsync();

            return ServiceResult<LoanDto>.Ok(await MapToDtoAsync(loan), "وام با موفقیت ثبت شد و اقساط آن ایجاد گردید.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.Error("خطا در ثبت وام", ex);
            return ServiceResult<LoanDto>.Fail("خطا در ثبت وام. لطفاً دوباره تلاش کنید.");
        }
    }

    public async Task<ServiceResult<InstallmentPaymentDto>> PayInstallmentAsync(CreateInstallmentPaymentDto dto)
    {
        var installment = await _context.LoanInstallments
            .FirstOrDefaultAsync(i => i.Id == dto.InstallmentId && !i.IsDeleted);
        if (installment is null)
            return ServiceResult<InstallmentPaymentDto>.Fail("قسط یافت نشد.");

        if (dto.Amount <= 0)
            return ServiceResult<InstallmentPaymentDto>.Fail("مبلغ پرداختی باید بزرگتر از صفر باشد.");

        var remaining = installment.Amount - installment.PaidAmount;
        if (remaining <= 0)
            return ServiceResult<InstallmentPaymentDto>.Fail("این قسط قبلاً به‌طور کامل پرداخت شده است.");
        if (dto.Amount > remaining)
            return ServiceResult<InstallmentPaymentDto>.Fail($"مبلغ پرداختی از مانده قسط ({remaining:N0}) بیشتر است. اضافه‌پرداخت مجاز نیست.");

        await using var transaction = await _context.BeginTransactionAsync();
        try
        {
            var payment = new InstallmentPayment
            {
                InstallmentId = dto.InstallmentId,
                PaymentDate = dto.PaymentDate.Date,
                Amount = dto.Amount,
                Description = dto.Description?.Trim(),
                CreatedAt = DateTime.Now,
                CreatedBy = _currentUser.UserName,
                IsDeleted = false
            };
            _context.InstallmentPayments.Add(payment);

            installment.PaidAmount += dto.Amount;
            installment.Status = SummaryCalculator.ComputeInstallmentStatus(
                installment.Amount, installment.PaidAmount, installment.DueDate, DateTime.Today);

            await _context.SaveChangesAsync();

            var loan = await _context.Loans.FirstAsync(l => l.Id == installment.LoanId);
            await _audit.LogAsync(AppConstants.AuditActions.InstallmentPaid, "LoanInstallment", installment.Id.ToString(),
                $"پرداخت قسط {installment.InstallmentNumber} وام {loan.Id} به مبلغ {dto.Amount:N0} ثبت شد.");

            await transaction.CommitAsync();
            return ServiceResult<InstallmentPaymentDto>.Ok(new InstallmentPaymentDto
            {
                Id = payment.Id,
                InstallmentId = payment.InstallmentId,
                PaymentDate = payment.PaymentDate,
                PaymentDatePersian = PersianDateHelper.ToPersianDate(payment.PaymentDate),
                Amount = payment.Amount,
                Description = payment.Description,
                CreatedBy = payment.CreatedBy
            }, "پرداخت قسط با موفقیت ثبت شد.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.Error("خطا در پرداخت قسط", ex);
            return ServiceResult<InstallmentPaymentDto>.Fail("خطا در ثبت پرداخت قسط.");
        }
    }

    public async Task<IReadOnlyList<LoanDto>> GetLoansAsync(int? memberId = null)
    {
        var query = _context.Loans.Where(l => !l.IsDeleted);
        if (memberId.HasValue)
            query = query.Where(l => l.MemberId == memberId.Value);

        var loans = await query.OrderByDescending(l => l.LoanDate).ThenByDescending(l => l.Id).ToListAsync();
        var result = new List<LoanDto>();
        foreach (var loan in loans)
            result.Add(await MapToDtoAsync(loan));
        return result;
    }

    public async Task<IReadOnlyList<LoanInstallmentDto>> GetInstallmentsAsync(int? memberId = null, int? loanId = null)
    {
        var query = _context.LoanInstallments.Where(i => !i.IsDeleted);
        if (memberId.HasValue)
            query = query.Where(i => i.Loan.MemberId == memberId.Value);
        if (loanId.HasValue)
            query = query.Where(i => i.LoanId == loanId.Value);

        var installments = await query
            .OrderBy(i => i.DueDate)
            .ThenBy(i => i.InstallmentNumber)
            .ToListAsync();

        var result = new List<LoanInstallmentDto>();
        foreach (var installment in installments)
            result.Add(await MapInstallmentToDtoAsync(installment));
        return result;
    }

    public async Task<IReadOnlyList<LoanInstallmentDto>> GetInstallmentsByStatusAsync(InstallmentStatus status)
    {
        var installments = await _context.LoanInstallments
            .Where(i => !i.IsDeleted && i.Status == status)
            .OrderBy(i => i.DueDate)
            .ToListAsync();

        var result = new List<LoanInstallmentDto>();
        foreach (var installment in installments)
            result.Add(await MapInstallmentToDtoAsync(installment));
        return result;
    }

    public Task<decimal> GetOutstandingLoanAsync(int memberId) =>
        SummaryCalculator.GetOutstandingLoanAsync(_context, memberId);

    public Task<decimal> GetOutstandingLoanAtAsync(int memberId, DateTime date) =>
        SummaryCalculator.GetOutstandingLoanAtAsync(_context, memberId, date);

    public Task<decimal> GetDueInstallmentsAsync(int memberId) =>
        SummaryCalculator.GetDueInstallmentsAsync(_context, memberId);

    public async Task RefreshStatusesAsync()
    {
        try
        {
            await SummaryCalculator.RefreshAllStatusesAsync(_context);
        }
        catch (Exception ex)
        {
            _logger.Error("خطا در به‌روزرسانی وضعیت اقساط", ex);
        }
    }

    private async Task<LoanDto> MapToDtoAsync(Loan loan)
    {
        var member = await _context.Members.IgnoreQueryFilters().FirstOrDefaultAsync(m => m.Id == loan.MemberId);
        var payments = await _context.InstallmentPayments
            .Where(p => !p.IsDeleted && p.Installment.LoanId == loan.Id)
            .ToListAsync();
        var paid = payments.Sum(p => p.Amount);
        return new LoanDto
        {
            Id = loan.Id,
            MemberId = loan.MemberId,
            MemberName = member?.FullName ?? string.Empty,
            MemberCode = member?.MemberCode ?? string.Empty,
            Amount = loan.Amount,
            LoanDate = loan.LoanDate,
            LoanDatePersian = PersianDateHelper.ToPersianDate(loan.LoanDate),
            InstallmentCount = loan.InstallmentCount,
            InstallmentAmount = loan.InstallmentAmount,
            PaidAmount = paid,
            RemainingAmount = Math.Max(loan.Amount - paid, 0),
            Description = loan.Description,
            CreatedBy = loan.CreatedBy,
            CreatedAt = loan.CreatedAt
        };
    }

    private async Task<LoanInstallmentDto> MapInstallmentToDtoAsync(LoanInstallment installment)
    {
        var loan = await _context.Loans.IgnoreQueryFilters().FirstAsync(l => l.Id == installment.LoanId);
        var member = await _context.Members.IgnoreQueryFilters().FirstOrDefaultAsync(m => m.Id == loan.MemberId);
        return new LoanInstallmentDto
        {
            Id = installment.Id,
            LoanId = installment.LoanId,
            MemberId = loan.MemberId,
            MemberName = member?.FullName ?? string.Empty,
            MemberCode = member?.MemberCode ?? string.Empty,
            InstallmentNumber = installment.InstallmentNumber,
            DueDate = installment.DueDate,
            DueDatePersian = PersianDateHelper.ToPersianDate(installment.DueDate),
            Amount = installment.Amount,
            PaidAmount = installment.PaidAmount,
            RemainingAmount = installment.Amount - installment.PaidAmount,
            Status = installment.Status
        };
    }
}
