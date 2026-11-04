using Microsoft.EntityFrameworkCore;
using TobiFund.Application.Common;
using TobiFund.Application.DTOs;
using TobiFund.Application.Interfaces;
using TobiFund.Domain.Entities;
using TobiFund.Domain.Enums;

namespace TobiFund.Application.Services;

/// <summary>ثبت تراکنش‌های سرمایه (تاریخچه کامل حفظ می‌شود؛ مقدار نهایی هرگز ذخیره نمی‌شود)</summary>
public class CapitalService : ICapitalService
{
    private readonly IApplicationDbContext _context;
    private readonly IAuditService _audit;
    private readonly ICurrentUserService _currentUser;
    private readonly IAppLogger _logger;

    public CapitalService(
        IApplicationDbContext context,
        IAuditService audit,
        ICurrentUserService currentUser,
        IAppLogger logger)
    {
        _context = context;
        _audit = audit;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<ServiceResult<CapitalTransactionDto>> AddTransactionAsync(CreateCapitalTransactionDto dto)
    {
        var member = await _context.Members.FirstOrDefaultAsync(m => m.Id == dto.MemberId && !m.IsDeleted);
        if (member is null)
            return ServiceResult<CapitalTransactionDto>.Fail("عضو یافت نشد.");
        if (member.Status != MemberStatus.Active)
            return ServiceResult<CapitalTransactionDto>.Fail("عضو غیرفعال است.");
        if (!Enum.IsDefined(dto.TransactionType))
            return ServiceResult<CapitalTransactionDto>.Fail("نوع تراکنش نامعتبر است.");
        if (dto.Amount <= 0)
            return ServiceResult<CapitalTransactionDto>.Fail("مبلغ باید بزرگتر از صفر باشد.");

        await using var transaction = await _context.BeginTransactionAsync();
        try
        {
            var entity = new CapitalTransaction
            {
                MemberId = dto.MemberId,
                TransactionDate = dto.TransactionDate.Date,
                Amount = dto.Amount,
                TransactionType = dto.TransactionType,
                Description = dto.Description?.Trim(),
                CreatedAt = DateTime.Now,
                CreatedBy = _currentUser.UserName,
                IsDeleted = false
            };

            _context.CapitalTransactions.Add(entity);
            await _context.SaveChangesAsync();

            await _audit.LogAsync(AppConstants.AuditActions.CapitalAdded, "CapitalTransaction", entity.Id.ToString(),
                $"ثبت سرمایه برای عضو {member.MemberCode}: {dto.Amount:N0} ({entity.TypeNameText()}) توسط {_currentUser.UserName}");

            await transaction.CommitAsync();
            return ServiceResult<CapitalTransactionDto>.Ok(MapToDto(entity, member), "تراکنش سرمایه با موفقیت ثبت شد.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.Error("خطا در ثبت تراکنش سرمایه", ex);
            return ServiceResult<CapitalTransactionDto>.Fail("خطا در ثبت تراکنش سرمایه.");
        }
    }

    public async Task<IReadOnlyList<CapitalTransactionDto>> GetMemberTransactionsAsync(int memberId)
    {
        var transactions = await _context.CapitalTransactions
            .Where(t => t.MemberId == memberId && !t.IsDeleted)
            .OrderByDescending(t => t.TransactionDate)
            .ThenByDescending(t => t.Id)
            .ToListAsync();
        var member = await _context.Members.FirstOrDefaultAsync(m => m.Id == memberId);
        return transactions.Select(t => MapToDto(t, member)).ToList();
    }

    public async Task<IReadOnlyList<CapitalTransactionDto>> GetAllAsync(DateTime? from = null, DateTime? to = null)
    {
        var query = _context.CapitalTransactions.Where(t => !t.IsDeleted);
        if (from.HasValue)
            query = query.Where(t => t.TransactionDate >= from.Value.Date);
        if (to.HasValue)
            query = query.Where(t => t.TransactionDate < to.Value.Date.AddDays(1));

        var transactions = await query
            .OrderByDescending(t => t.TransactionDate)
            .ThenByDescending(t => t.Id)
            .ToListAsync();

        var memberIds = transactions.Select(t => t.MemberId).Distinct().ToList();
        var members = await _context.Members
            .IgnoreQueryFilters()
            .Where(m => memberIds.Contains(m.Id))
            .ToDictionaryAsync(m => m.Id);

        return transactions
            .Select(t => MapToDto(t, members.GetValueOrDefault(t.MemberId)))
            .ToList();
    }

    public Task<decimal> GetBalanceAsync(int memberId) =>
        SummaryCalculator.GetCapitalAsync(_context, memberId);

    public Task<decimal> GetBalanceAtAsync(int memberId, DateTime date) =>
        SummaryCalculator.GetCapitalAtAsync(_context, memberId, date);

    private static CapitalTransactionDto MapToDto(CapitalTransaction t, Member? member) => new()
    {
        Id = t.Id,
        MemberId = t.MemberId,
        MemberName = member?.FullName ?? string.Empty,
        MemberCode = member?.MemberCode ?? string.Empty,
        TransactionDate = t.TransactionDate,
        TransactionDatePersian = PersianDateHelper.ToPersianDate(t.TransactionDate),
        Amount = t.Amount,
        TransactionType = t.TransactionType,
        Description = t.Description,
        CreatedBy = t.CreatedBy,
        CreatedAt = t.CreatedAt
    };
}

internal static class CapitalTransactionExtensions
{
    public static string TypeNameText(this CapitalTransaction t) => t.TransactionType switch
    {
        TransactionType.Deposit => "واریز",
        TransactionType.Withdrawal => "برداشت",
        TransactionType.Adjustment => "تعدیل",
        _ => "نامشخص"
    };
}
