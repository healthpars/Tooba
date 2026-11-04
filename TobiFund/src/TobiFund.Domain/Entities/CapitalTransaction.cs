using TobiFund.Domain.Enums;

namespace TobiFund.Domain.Entities;

/// <summary>تراکنش سرمایه عضو</summary>
public class CapitalTransaction
{
    public int Id { get; set; }

    public int MemberId { get; set; }

    /// <summary>تاریخ تراکنش (Gregorian)</summary>
    public DateTime TransactionDate { get; set; } = DateTime.Today;

    /// <summary>مبلغ (همیشه بزرگتر از صفر)</summary>
    public decimal Amount { get; set; }

    public TransactionType TransactionType { get; set; } = TransactionType.Deposit;

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public string? CreatedBy { get; set; }

    public bool IsDeleted { get; set; } = false;

    // Navigation
    public Member Member { get; set; } = null!;
}
