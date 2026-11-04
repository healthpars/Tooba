using TobiFund.Domain.Enums;

namespace TobiFund.Domain.Entities;

/// <summary>آبونمان ماهانه عضو</summary>
public class Subscription
{
    public int Id { get; set; }

    public int MemberId { get; set; }

    /// <summary>سال شمسی</summary>
    public int PersianYear { get; set; }

    /// <summary>ماه شمسی (1 تا 12)</summary>
    public int PersianMonth { get; set; }

    /// <summary>تاریخ سررسید (Gregorian)</summary>
    public DateTime DueDate { get; set; }

    public decimal Amount { get; set; } = 150000;

    public decimal PaidAmount { get; set; } = 0;

    public decimal RemainingAmount => Amount - PaidAmount;

    public PaymentStatus Status { get; set; } = PaymentStatus.Unpaid;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public bool IsDeleted { get; set; } = false;

    // Navigation
    public Member Member { get; set; } = null!;
    public ICollection<SubscriptionPayment> Payments { get; set; } = new List<SubscriptionPayment>();
}
