using TobiFund.Domain.Enums;

namespace TobiFund.Domain.Entities;

/// <summary>عضو صندوق</summary>
public class Member
{
    public int Id { get; set; }

    /// <summary>شناسه عضویت (یکتا)</summary>
    public string MemberCode { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    /// <summary>نام کامل (محاسبه‌ای)</summary>
    public string FullName => $"{FirstName} {LastName}".Trim();

    public string? Phone { get; set; }

    /// <summary>تاریخ عضویت (Gregorian در دیتابیس، شمسی در نمایش)</summary>
    public DateTime JoinDate { get; set; } = DateTime.Today;

    public MemberStatus Status { get; set; } = MemberStatus.Active;

    public string? Notes { get; set; }

    /// <summary>حذف منطقی (Soft Delete)</summary>
    public bool IsDeleted { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public string? CreatedBy { get; set; }

    // Navigation Properties
    public ICollection<CapitalTransaction> CapitalTransactions { get; set; } = new List<CapitalTransaction>();
    public ICollection<Loan> Loans { get; set; } = new List<Loan>();
    public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
    public ICollection<DailyScore> DailyScores { get; set; } = new List<DailyScore>();
}
