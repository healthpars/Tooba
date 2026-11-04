namespace TobiFund.Application.DTOs;

public class DailyScoreDto
{
    public int Id { get; set; }
    public int MemberId { get; set; }
    public DateTime ScoreDate { get; set; }
    public string ScoreDatePersian { get; set; } = string.Empty;
    public decimal CapitalAmount { get; set; }
    public decimal LoanAmount { get; set; }
    public int PositiveScore { get; set; }
    public int NegativeScore { get; set; }
    public int NetScore { get; set; }
}

/// <summary>ردیف جدول رتبه‌بندی اعضا</summary>
public class RankingDto
{
    public int Rank { get; set; }
    public int MemberId { get; set; }
    public string MemberCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public decimal TotalCapital { get; set; }
    public decimal OutstandingLoan { get; set; }
    public decimal DueInstallments { get; set; }
    public decimal DueSubscription { get; set; }
    public int TotalScore { get; set; }
}
