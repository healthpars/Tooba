namespace TobiFund.Domain.Entities;

/// <summary>وام عضو</summary>
public class Loan
{
    public int Id { get; set; }

    public int MemberId { get; set; }

    /// <summary>مبلغ کل وام</summary>
    public decimal Amount { get; set; }

    /// <summary>تاریخ دریافت وام</summary>
    public DateTime LoanDate { get; set; } = DateTime.Today;

    public int InstallmentCount { get; set; } = 10;

    /// <summary>مبلغ هر قسط (مبلغ پایه؛ مبلغ قسط آخر ممکن است متفاوت باشد)</summary>
    public decimal InstallmentAmount { get; set; }

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public string? CreatedBy { get; set; }

    public bool IsDeleted { get; set; } = false;

    // Navigation
    public Member Member { get; set; } = null!;
    public ICollection<LoanInstallment> Installments { get; set; } = new List<LoanInstallment>();
}
