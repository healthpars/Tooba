using TobiFund.Application.DTOs;
using TobiFund.Domain.Enums;

namespace TobiFund.Application.Interfaces;

public interface IReportService
{
    Task<IReadOnlyList<MemberDto>> MembersReportAsync(string? search = null);
    Task<IReadOnlyList<CapitalTransactionDto>> CapitalReportAsync(int? memberId = null, DateTime? from = null, DateTime? to = null);
    Task<IReadOnlyList<LoanDto>> LoansReportAsync(int? memberId = null);
    Task<IReadOnlyList<LoanInstallmentDto>> InstallmentsReportAsync(InstallmentStatus? status = null, int? memberId = null);
    Task<IReadOnlyList<SubscriptionDto>> SubscriptionsReportAsync(PaymentStatus? status = null, int? memberId = null);
    Task<IReadOnlyList<RankingDto>> ScoresReportAsync();
    Task<IReadOnlyList<AuditLogDto>> AuditReportAsync(DateTime? from = null, DateTime? to = null);
}
