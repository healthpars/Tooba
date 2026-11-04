namespace TobiFund.Application.DTOs;

public class DashboardStatsDto
{
    public int TotalMembers { get; set; }
    public int ActiveMembers { get; set; }
    public decimal TotalCapital { get; set; }
    public decimal TotalLoans { get; set; }
    public decimal TotalDueInstallments { get; set; }
    public decimal TotalDueSubscriptions { get; set; }
    public long TotalScore { get; set; }
}

public enum AlertType
{
    OverdueInstallment,
    OverdueSubscription,
    DebtorMember,
    UpcomingInstallment
}

public class AlertDto
{
    public AlertType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public int MemberId { get; set; }
    public string MemberCode { get; set; } = string.Empty;
    public string MemberName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime? DueDate { get; set; }
    public string DueDatePersian { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class DashboardDto
{
    public DashboardStatsDto Stats { get; set; } = new();
    public IReadOnlyList<RankingDto> TopMembers { get; set; } = Array.Empty<RankingDto>();
    public IReadOnlyList<AlertDto> Alerts { get; set; } = Array.Empty<AlertDto>();
}
