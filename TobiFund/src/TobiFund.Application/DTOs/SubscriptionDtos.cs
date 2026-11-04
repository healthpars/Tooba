using TobiFund.Application.Common;
using TobiFund.Domain.Enums;

namespace TobiFund.Application.DTOs;

public class SubscriptionDto
{
    public int Id { get; set; }
    public int MemberId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public string MemberCode { get; set; } = string.Empty;
    public int PersianYear { get; set; }
    public int PersianMonth { get; set; }
    public string MonthName => PersianDateHelper.PersianMonthName(PersianMonth);
    public string YearMonth => PersianDateHelper.ToPersianDigits($"{PersianYear}/{PersianMonth:00}");
    public DateTime DueDate { get; set; }
    public string DueDatePersian { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public PaymentStatus Status { get; set; }
    public string StatusName => Status switch
    {
        PaymentStatus.Unpaid => "پرداخت نشده",
        PaymentStatus.PartiallyPaid => "پرداخت جزئی",
        PaymentStatus.Paid => "پرداخت شده",
        _ => "نامشخص"
    };
    public bool IsOverdue => Status != PaymentStatus.Paid && DueDate.Date < DateTime.Today;
}

public class SubscriptionPaymentDto
{
    public int Id { get; set; }
    public int SubscriptionId { get; set; }
    public DateTime PaymentDate { get; set; }
    public string PaymentDatePersian { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public string? CreatedBy { get; set; }
}

public class CreateSubscriptionPaymentDto
{
    public int SubscriptionId { get; set; }
    public DateTime PaymentDate { get; set; } = DateTime.Today;
    public decimal Amount { get; set; }
    public string? Description { get; set; }
}
