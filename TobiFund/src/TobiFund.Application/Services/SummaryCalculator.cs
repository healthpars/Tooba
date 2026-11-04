using Microsoft.EntityFrameworkCore;
using TobiFund.Application.DTOs;
using TobiFund.Application.Interfaces;
using TobiFund.Domain.Entities;
using TobiFund.Domain.Enums;

namespace TobiFund.Application.Services;

/// <summary>
/// منطق مالی مرکزی برنامه.
/// تمام محاسبات «موجودی فعلی» از روی تراکنش‌ها محاسبه می‌شوند (هیچ مقدار نهایی ذخیره نمی‌شود)
/// و محاسبات Snapshot برای تاریخ‌های گذشته بازسازی می‌شوند.
/// </summary>
public static class SummaryCalculator
{
    /// <summary>سرمایه کل عضو (موجودی فعلی)</summary>
    public static async Task<decimal> GetCapitalAsync(IApplicationDbContext context, int memberId)
    {
        var transactions = await context.CapitalTransactions
            .Where(t => t.MemberId == memberId && !t.IsDeleted)
            .ToListAsync();
        return transactions.Sum(t => t.TransactionType == TransactionType.Withdrawal ? -t.Amount : t.Amount);
    }

    /// <summary>سرمایه عضو تا پایان یک روز مشخص (برای Snapshot روزانه)</summary>
    public static async Task<decimal> GetCapitalAtAsync(IApplicationDbContext context, int memberId, DateTime date)
    {
        var nextDay = date.Date.AddDays(1);
        var transactions = await context.CapitalTransactions
            .Where(t => t.MemberId == memberId && !t.IsDeleted && t.TransactionDate < nextDay)
            .ToListAsync();
        return transactions.Sum(t => t.TransactionType == TransactionType.Withdrawal ? -t.Amount : t.Amount);
    }

    /// <summary>مجموع وام‌های فعال عضو (مبلغ کل وام‌های پرداخت‌شده)</summary>
    public static async Task<decimal> GetTotalLoansAsync(IApplicationDbContext context, int memberId)
    {
        var loans = await context.Loans
            .Where(l => l.MemberId == memberId && !l.IsDeleted)
            .ToListAsync();
        return loans.Sum(l => l.Amount);
    }

    /// <summary>مانده وام فعلی عضو (مبلغ وام منهای پرداخت‌های انجام‌شده)</summary>
    public static async Task<decimal> GetOutstandingLoanAsync(IApplicationDbContext context, int memberId)
    {
        var loans = await context.Loans
            .Where(l => l.MemberId == memberId && !l.IsDeleted)
            .ToListAsync();
        decimal outstanding = 0m;
        foreach (var loan in loans)
        {
            var paid = await context.InstallmentPayments
                .Where(p => !p.IsDeleted && p.Installment.LoanId == loan.Id)
                .ToListAsync();
            outstanding += loan.Amount - paid.Sum(p => p.Amount);
        }
        return Math.Max(outstanding, 0m);
    }

    /// <summary>مانده وام عضو تا پایان یک روز مشخص (بازسازی تاریخی برای Snapshot)</summary>
    public static async Task<decimal> GetOutstandingLoanAtAsync(IApplicationDbContext context, int memberId, DateTime date)
    {
        var nextDay = date.Date.AddDays(1);
        var loans = await context.Loans
            .Where(l => l.MemberId == memberId && !l.IsDeleted && l.LoanDate < nextDay)
            .ToListAsync();
        decimal outstanding = 0m;
        foreach (var loan in loans)
        {
            var paid = await context.InstallmentPayments
                .Where(p => !p.IsDeleted && p.PaymentDate < nextDay && p.Installment.LoanId == loan.Id)
                .ToListAsync();
            outstanding += loan.Amount - paid.Sum(p => p.Amount);
        }
        return Math.Max(outstanding, 0m);
    }

    /// <summary>مجموع اقساط ماهانه (وام‌های فعال)</summary>
    public static async Task<decimal> GetMonthlyInstallmentAsync(IApplicationDbContext context, int memberId)
    {
        var loans = await context.Loans
            .Where(l => l.MemberId == memberId && !l.IsDeleted)
            .ToListAsync();
        return loans.Sum(l => l.InstallmentAmount);
    }

    /// <summary>
    /// اقساط سررسیدشده پرداخت‌نشده:
    /// SUM(Amount - PaidAmount) برای اقساطی که DueDate &lt;= امروز و مانده &gt; 0 دارند.
    /// </summary>
    public static async Task<decimal> GetDueInstallmentsAsync(IApplicationDbContext context, int memberId, DateTime? today = null)
    {
        var now = (today ?? DateTime.Today).Date;
        var nextDay = now.AddDays(1);
        var installments = await context.LoanInstallments
            .Where(i => !i.IsDeleted && i.Loan.MemberId == memberId && i.DueDate < nextDay)
            .ToListAsync();
        return installments.Sum(i => i.Amount - i.PaidAmount);
    }

    /// <summary>
    /// آبونمان معوق: SUM(Amount - PaidAmount) برای آبونمان‌هایی که DueDate &lt;= امروز و مانده &gt; 0 دارند.
    /// </summary>
    public static async Task<decimal> GetDueSubscriptionAsync(IApplicationDbContext context, int memberId, DateTime? today = null)
    {
        var now = (today ?? DateTime.Today).Date;
        var nextDay = now.AddDays(1);
        var subscriptions = await context.Subscriptions
            .Where(s => !s.IsDeleted && s.MemberId == memberId && s.DueDate < nextDay)
            .ToListAsync();
        return subscriptions.Sum(s => s.Amount - s.PaidAmount);
    }

    /// <summary>امتیاز کل عضو = SUM(DailyScores.NetScore)</summary>
    public static async Task<int> GetTotalScoreAsync(IApplicationDbContext context, int memberId)
    {
        var scores = await context.DailyScores
            .Where(s => s.MemberId == memberId)
            .ToListAsync();
        return scores.Sum(s => s.NetScore);
    }

    /// <summary>رتبه عضو (۱ = بیشترین امتیاز)</summary>
    public static async Task<int> GetRankAsync(IApplicationDbContext context, int memberId)
    {
        var members = await context.Members
            .Where(m => !m.IsDeleted)
            .ToListAsync();
        var memberScores = new Dictionary<int, int>();
        foreach (var member in members)
            memberScores[member.Id] = await GetTotalScoreAsync(context, member.Id);

        var myScore = memberScores.TryGetValue(memberId, out var s) ? s : 0;
        return memberScores.Count(kv => kv.Value > myScore) + 1;
    }

    /// <summary>خلاصه کامل صفحه جزئیات عضو</summary>
    public static async Task<MemberSummaryDto> BuildSummaryAsync(IApplicationDbContext context, Member member)
    {
        return new MemberSummaryDto
        {
            MemberId = member.Id,
            MemberCode = member.MemberCode,
            FullName = member.FullName,
            TotalCapital = await GetCapitalAsync(context, member.Id),
            TotalLoan = await GetTotalLoansAsync(context, member.Id),
            OutstandingLoan = await GetOutstandingLoanAsync(context, member.Id),
            MonthlyInstallment = await GetMonthlyInstallmentAsync(context, member.Id),
            DueInstallments = await GetDueInstallmentsAsync(context, member.Id),
            DueSubscription = await GetDueSubscriptionAsync(context, member.Id),
            TotalScore = await GetTotalScoreAsync(context, member.Id),
            Rank = await GetRankAsync(context, member.Id)
        };
    }

    /// <summary>محاسبه وضعیت قسط بر اساس پرداخت‌ها و تاریخ سررسید</summary>
    public static InstallmentStatus ComputeInstallmentStatus(decimal amount, decimal paidAmount, DateTime dueDate, DateTime today)
    {
        if (amount > 0 && paidAmount >= amount) return InstallmentStatus.Paid;
        if (paidAmount > 0) return InstallmentStatus.PartiallyPaid;
        var dueDay = dueDate.Date;
        var todayDate = today.Date;
        if (dueDay < todayDate) return InstallmentStatus.Overdue;
        if (dueDay == todayDate) return InstallmentStatus.Due;
        return InstallmentStatus.Future;
    }

    /// <summary>محاسبه وضعیت پرداخت آبونمان</summary>
    public static PaymentStatus ComputePaymentStatus(decimal amount, decimal paidAmount)
    {
        if (amount > 0 && paidAmount >= amount) return PaymentStatus.Paid;
        if (paidAmount > 0) return PaymentStatus.PartiallyPaid;
        return PaymentStatus.Unpaid;
    }

    /// <summary>به‌روزرسانی وضعیت تمام اقساط و آبونمان‌ها بر اساس امروز</summary>
    public static async Task<int> RefreshAllStatusesAsync(IApplicationDbContext context, DateTime? today = null)
    {
        var now = (today ?? DateTime.Today).Date;
        var installments = await context.LoanInstallments.Where(i => !i.IsDeleted).ToListAsync();
        var subscriptions = await context.Subscriptions.Where(s => !s.IsDeleted).ToListAsync();
        var changed = 0;

        foreach (var installment in installments)
        {
            var status = ComputeInstallmentStatus(installment.Amount, installment.PaidAmount, installment.DueDate, now);
            if (installment.Status != status)
            {
                installment.Status = status;
                changed++;
            }
        }

        foreach (var subscription in subscriptions)
        {
            var status = ComputePaymentStatus(subscription.Amount, subscription.PaidAmount);
            if (subscription.Status != status)
            {
                subscription.Status = status;
                changed++;
            }
        }

        if (changed > 0)
            await context.SaveChangesAsync();

        return changed;
    }
}
