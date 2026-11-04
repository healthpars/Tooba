namespace TobiFund.Domain.Entities;

/// <summary>پرداخت آبونمان</summary>
public class SubscriptionPayment
{
    public int Id { get; set; }

    public int SubscriptionId { get; set; }

    public DateTime PaymentDate { get; set; } = DateTime.Today;

    public decimal Amount { get; set; }

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public string? CreatedBy { get; set; }

    public bool IsDeleted { get; set; } = false;

    // Navigation
    public Subscription Subscription { get; set; } = null!;
}
