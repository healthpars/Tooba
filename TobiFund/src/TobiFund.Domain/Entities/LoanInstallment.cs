using TobiFund.Domain.Enums;

namespace TobiFund.Domain.Entities;

/// <summary>قسط وام</summary>
public class LoanInstallment
{
    public int Id { get; set; }

    public int LoanId { get; set; }

    public int InstallmentNumber { get; set; }

    /// <summary>تاریخ سررسید</summary>
    public DateTime DueDate { get; set; }

    public decimal Amount { get; set; }

    public decimal PaidAmount { get; set; } = 0;

    /// <summary>مانده = مبلغ - پرداخت شده</summary>
    public decimal RemainingAmount => Amount - PaidAmount;

    public InstallmentStatus Status { get; set; } = InstallmentStatus.Future;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public bool IsDeleted { get; set; } = false;

    // Navigation
    public Loan Loan { get; set; } = null!;
    public ICollection<InstallmentPayment> Payments { get; set; } = new List<InstallmentPayment>();
}
