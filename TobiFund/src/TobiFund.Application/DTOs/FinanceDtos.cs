using TobiFund.Domain.Enums;

namespace TobiFund.Application.DTOs;

public class CapitalTransactionDto
{
    public int Id { get; set; }
    public int MemberId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public string MemberCode { get; set; } = string.Empty;
    public DateTime TransactionDate { get; set; }
    public string TransactionDatePersian { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public TransactionType TransactionType { get; set; }
    public string TypeName => TransactionType switch
    {
        TransactionType.Deposit => "واریز",
        TransactionType.Withdrawal => "برداشت",
        TransactionType.Adjustment => "تعدیل",
        _ => "نامشخص"
    };
    public string? Description { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateCapitalTransactionDto
{
    public int MemberId { get; set; }
    public DateTime TransactionDate { get; set; } = DateTime.Today;
    public decimal Amount { get; set; }
    public TransactionType TransactionType { get; set; } = TransactionType.Deposit;
    public string? Description { get; set; }
}

public class LoanDto
{
    public int Id { get; set; }
    public int MemberId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public string MemberCode { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime LoanDate { get; set; }
    public string LoanDatePersian { get; set; } = string.Empty;
    public int InstallmentCount { get; set; }
    public decimal InstallmentAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public string? Description { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateLoanDto
{
    public int MemberId { get; set; }
    public decimal Amount { get; set; }
    public DateTime LoanDate { get; set; } = DateTime.Today;
    public int InstallmentCount { get; set; } = 10;
    public string? Description { get; set; }
}

public class LoanInstallmentDto
{
    public int Id { get; set; }
    public int LoanId { get; set; }
    public int MemberId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public string MemberCode { get; set; } = string.Empty;
    public int InstallmentNumber { get; set; }
    public DateTime DueDate { get; set; }
    public string DueDatePersian { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public InstallmentStatus Status { get; set; }
    public string StatusName => Status switch
    {
        InstallmentStatus.Future => "آینده",
        InstallmentStatus.Due => "سررسید",
        InstallmentStatus.PartiallyPaid => "پرداخت جزئی",
        InstallmentStatus.Paid => "پرداخت شده",
        InstallmentStatus.Overdue => "معوق",
        _ => "نامشخص"
    };
    public bool IsOverdue => Status == InstallmentStatus.Overdue;
}

public class InstallmentPaymentDto
{
    public int Id { get; set; }
    public int InstallmentId { get; set; }
    public DateTime PaymentDate { get; set; }
    public string PaymentDatePersian { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public string? CreatedBy { get; set; }
}

public class CreateInstallmentPaymentDto
{
    public int InstallmentId { get; set; }
    public DateTime PaymentDate { get; set; } = DateTime.Today;
    public decimal Amount { get; set; }
    public string? Description { get; set; }
}
