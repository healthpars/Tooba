using TobiFund.Domain.Enums;

namespace TobiFund.Application.DTOs;

public class MemberDto
{
    public int Id { get; set; }
    public string MemberCode { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}".Trim();
    public string? Phone { get; set; }
    public DateTime JoinDate { get; set; }
    public string JoinDatePersian { get; set; } = string.Empty;
    public MemberStatus Status { get; set; }
    public string StatusName => Status == MemberStatus.Active ? "فعال" : "غیرفعال";
    public string? Notes { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }

    // مقادیر محاسبه‌ای (Current Balance — قابل محاسبه از تراکنش‌ها)
    public decimal TotalCapital { get; set; }
    public decimal OutstandingLoan { get; set; }
    public decimal DueInstallments { get; set; }
    public decimal DueSubscription { get; set; }
    public int TotalScore { get; set; }
    public int Rank { get; set; }
}

public class CreateMemberDto
{
    public string MemberCode { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public DateTime JoinDate { get; set; } = DateTime.Today;
    public string? Notes { get; set; }
}

public class UpdateMemberDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public MemberStatus Status { get; set; }
    public string? Notes { get; set; }
}

/// <summary>خلاصه صفحه جزئیات عضو ( according to سند طراحی)</summary>
public class MemberSummaryDto
{
    public int MemberId { get; set; }
    public string MemberCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public decimal TotalCapital { get; set; }
    public decimal TotalLoan { get; set; }
    public decimal OutstandingLoan { get; set; }
    public decimal MonthlyInstallment { get; set; }
    public decimal DueInstallments { get; set; }
    public decimal DueSubscription { get; set; }
    public int TotalScore { get; set; }
    public int Rank { get; set; }
}
