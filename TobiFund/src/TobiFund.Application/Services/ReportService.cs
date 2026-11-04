using TobiFund.Application.DTOs;
using TobiFund.Application.Interfaces;
using TobiFund.Domain.Enums;

namespace TobiFund.Application.Services;

/// <summary>سرویس گزارش‌گیری (منبع داده گزارش‌ها)</summary>
public class ReportService : IReportService
{
    private readonly IMemberService _members;
    private readonly ICapitalService _capital;
    private readonly ILoanService _loans;
    private readonly ISubscriptionService _subscriptions;
    private readonly IScoreService _scores;
    private readonly IAuditService _audit;

    public ReportService(
        IMemberService members,
        ICapitalService capital,
        ILoanService loans,
        ISubscriptionService subscriptions,
        IScoreService scores,
        IAuditService audit)
    {
        _members = members;
        _capital = capital;
        _loans = loans;
        _subscriptions = subscriptions;
        _scores = scores;
        _audit = audit;
    }

    public Task<IReadOnlyList<MemberDto>> MembersReportAsync(string? search = null) =>
        _members.GetAllAsync(search);

    public Task<IReadOnlyList<CapitalTransactionDto>> CapitalReportAsync(int? memberId = null, DateTime? from = null, DateTime? to = null) =>
        memberId.HasValue
            ? _capital.GetMemberTransactionsAsync(memberId.Value)
            : _capital.GetAllAsync(from, to);

    public Task<IReadOnlyList<LoanDto>> LoansReportAsync(int? memberId = null) =>
        _loans.GetLoansAsync(memberId);

    public async Task<IReadOnlyList<LoanInstallmentDto>> InstallmentsReportAsync(InstallmentStatus? status = null, int? memberId = null)
    {
        if (status.HasValue)
        {
            var byStatus = await _loans.GetInstallmentsByStatusAsync(status.Value);
            return memberId.HasValue
                ? byStatus.Where(i => i.MemberId == memberId.Value).ToList()
                : byStatus;
        }
        return await _loans.GetInstallmentsAsync(memberId);
    }

    public async Task<IReadOnlyList<SubscriptionDto>> SubscriptionsReportAsync(PaymentStatus? status = null, int? memberId = null)
    {
        var all = await _subscriptions.GetSubscriptionsAsync(memberId);
        return status.HasValue
            ? all.Where(s => s.Status == status.Value).ToList()
            : all;
    }

    public Task<IReadOnlyList<RankingDto>> ScoresReportAsync() =>
        _scores.GetRankingAsync();

    public Task<IReadOnlyList<AuditLogDto>> AuditReportAsync(DateTime? from = null, DateTime? to = null) =>
        _audit.GetAllAsync(from, to);
}
