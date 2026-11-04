namespace TobiFund.Domain.Entities;

/// <summary>پرداخت قسط</summary>
public class InstallmentPayment
{
    public int Id { get; set; }

    public int InstallmentId { get; set; }

    public DateTime PaymentDate { get; set; } = DateTime.Today;

    public decimal Amount { get; set; }

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public string? CreatedBy { get; set; }

    public bool IsDeleted { get; set; } = false;

    // Navigation
    public LoanInstallment Installment { get; set; } = null!;
}
