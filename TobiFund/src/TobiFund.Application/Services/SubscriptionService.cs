using Microsoft.EntityFrameworkCore;
using TobiFund.Application.Common;
using TobiFund.Application.DTOs;
using TobiFund.Application.Interfaces;
using TobiFund.Domain.Entities;
using TobiFund.Domain.Enums;

namespace TobiFund.Application.Services;

/// <summary>مدیریت آبونمان ماهانه اعضا (۱۲ آبونمان در هر سال شمسی)</summary>
public class SubscriptionService : ISubscriptionService
{
    private readonly IApplicationDbContext _context;
    private readonly IAuditService _audit;
    private readonly ICurrentUserService _currentUser;
    private readonly ISettingsService _settings;
    private readonly IAppLogger _logger;

    public SubscriptionService(
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

    public async Task<ServiceResult<int>> EnsureSubscriptionsAsync(int memberId, int persianYear)
    {
        var member = await _context.Members.FirstOrDefaultAsync(m => m.Id == memberId && !m.IsDeleted);
        if (member is null)
            return ServiceResult<int>.Fail("عضو یافت نشد.");
        if (persianYear < 1300 || persianYear > 1500)
            return ServiceResult<int>.Fail("سال شمسی نامعتبر است.");

        var amount = await _settings.GetDecimalAsync(SettingKeys.DefaultSubscriptionAmount, 150000m);
        if (amount <= 0)
            return ServiceResult<int>.Fail("مبلغ پیش‌فرض آبونمان باید بزرگتر از صفر باشد.");

        var existingMonths = await _context.Subscriptions
            .Where(s => s.MemberId == memberId && s.PersianYear == persianYear && !s.IsDeleted)
            .Select(s => s.PersianMonth)
            .ToListAsync();
        var existingSet = existingMonths.ToHashSet();

        await using var transaction = await _context.BeginTransactionAsync();
        try
        {
            var created = 0;
            for (var month = 1; month <= 12; month++)
            {
                if (existingSet.Contains(month)) continue;
                var dueDate = PersianDateHelper.PersianToGregorian(persianYear, month, 1);
                _context.Subscriptions.Add(new Subscription
                {
                    MemberId = memberId,
                    PersianYear = persianYear,
                    PersianMonth = month,
                    DueDate = dueDate,
                    Amount = amount,
                    PaidAmount = 0,
                    Status = PaymentStatus.Unpaid,
                    CreatedAt = DateTime.Now
                });
                created++;
            }

            if (created > 0)
                await _context.SaveChangesAsync();

            await transaction.CommitAsync();
            return ServiceResult<int>.Ok(created,
                created > 0 ? $"{created} رکورد آبونمان برای سال {persianYear} ایجاد شد." : "رکوردهای آبونمان این سال قبلاً ایجاد شده‌اند.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.Error("خطا در ایجاد آبونمان", ex);
            return ServiceResult<int>.Fail("خطا در ایجاد رکوردهای آبونمان.");
        }
    }

    public async Task<ServiceResult<SubscriptionPaymentDto>> PaySubscriptionAsync(CreateSubscriptionPaymentDto dto)
    {
        var subscription = await _context.Subscriptions
            .FirstOrDefaultAsync(s => s.Id == dto.SubscriptionId && !s.IsDeleted);
        if (subscription is null)
            return ServiceResult<SubscriptionPaymentDto>.Fail("آبونمان یافت نشد.");

        if (dto.Amount <= 0)
            return ServiceResult<SubscriptionPaymentDto>.Fail("مبلغ پرداختی باید بزرگتر از صفر باشد.");

        var remaining = subscription.Amount - subscription.PaidAmount;
        if (remaining <= 0)
            return ServiceResult<SubscriptionPaymentDto>.Fail("این آبونمان قبلاً به‌طور کامل پرداخت شده است.");
        if (dto.Amount > remaining)
            return ServiceResult<SubscriptionPaymentDto>.Fail($"مبلغ پرداختی از مانده آبونمان ({remaining:N0}) بیشتر است. اضافه‌پرداخت مجاز نیست.");

        await using var transaction = await _context.BeginTransactionAsync();
        try
        {
            var payment = new SubscriptionPayment
            {
                SubscriptionId = dto.SubscriptionId,
                PaymentDate = dto.PaymentDate.Date,
                Amount = dto.Amount,
                Description = dto.Description?.Trim(),
                CreatedAt = DateTime.Now,
                CreatedBy = _currentUser.UserName,
                IsDeleted = false
            };
            _context.SubscriptionPayments.Add(payment);

            subscription.PaidAmount += dto.Amount;
            subscription.Status = SummaryCalculator.ComputePaymentStatus(subscription.Amount, subscription.PaidAmount);

            await _context.SaveChangesAsync();

            var member = await _context.Members.IgnoreQueryFilters().FirstAsync(m => m.Id == subscription.MemberId);
            await _audit.LogAsync(AppConstants.AuditActions.SubscriptionPaid, "Subscription", subscription.Id.ToString(),
                $"پرداخت آبونمان {subscription.PersianYear}/{subscription.PersianMonth:00} عضو {member.MemberCode} به مبلغ {dto.Amount:N0} ثبت شد.");

            await transaction.CommitAsync();
            return ServiceResult<SubscriptionPaymentDto>.Ok(new SubscriptionPaymentDto
            {
                Id = payment.Id,
                SubscriptionId = payment.SubscriptionId,
                PaymentDate = payment.PaymentDate,
                PaymentDatePersian = PersianDateHelper.ToPersianDate(payment.PaymentDate),
                Amount = payment.Amount,
                Description = payment.Description,
                CreatedBy = payment.CreatedBy
            }, "پرداخت آبونمان با موفقیت ثبت شد.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.Error("خطا در پرداخت آبونمان", ex);
            return ServiceResult<SubscriptionPaymentDto>.Fail("خطا در ثبت پرداخت آبونمان.");
        }
    }

    public async Task<IReadOnlyList<SubscriptionDto>> GetSubscriptionsAsync(int? memberId = null, int? persianYear = null)
    {
        var query = _context.Subscriptions.Where(s => !s.IsDeleted);
        if (memberId.HasValue)
            query = query.Where(s => s.MemberId == memberId.Value);
        if (persianYear.HasValue)
            query = query.Where(s => s.PersianYear == persianYear.Value);

        var subscriptions = await query
            .OrderBy(s => s.DueDate)
            .ThenBy(s => s.PersianMonth)
            .ToListAsync();

        var memberIds = subscriptions.Select(s => s.MemberId).Distinct().ToList();
        var members = await _context.Members
            .IgnoreQueryFilters()
            .Where(m => memberIds.Contains(m.Id))
            .ToDictionaryAsync(m => m.Id);

        return subscriptions.Select(s => MapToDto(s, members.GetValueOrDefault(s.MemberId))).ToList();
    }

    public async Task<decimal> GetDueSubscriptionAsync(int memberId) =>
        await SummaryCalculator.GetDueSubscriptionAsync(_context, memberId);

    public async Task<IReadOnlyList<SubscriptionPaymentDto>> GetPaymentsAsync(int subscriptionId)
    {
        var payments = await _context.SubscriptionPayments
            .Where(p => p.SubscriptionId == subscriptionId && !p.IsDeleted)
            .OrderByDescending(p => p.PaymentDate)
            .ToListAsync();
        return payments.Select(p => new SubscriptionPaymentDto
        {
            Id = p.Id,
            SubscriptionId = p.SubscriptionId,
            PaymentDate = p.PaymentDate,
            PaymentDatePersian = PersianDateHelper.ToPersianDate(p.PaymentDate),
            Amount = p.Amount,
            Description = p.Description,
            CreatedBy = p.CreatedBy
        }).ToList();
    }

    private static SubscriptionDto MapToDto(Subscription s, Member? member) => new()
    {
        Id = s.Id,
        MemberId = s.MemberId,
        MemberName = member?.FullName ?? string.Empty,
        MemberCode = member?.MemberCode ?? string.Empty,
        PersianYear = s.PersianYear,
        PersianMonth = s.PersianMonth,
        DueDate = s.DueDate,
        DueDatePersian = PersianDateHelper.ToPersianDate(s.DueDate),
        Amount = s.Amount,
        PaidAmount = s.PaidAmount,
        RemainingAmount = s.Amount - s.PaidAmount,
        Status = s.Status
    };
}
