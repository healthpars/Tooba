namespace TobiFund.Domain.Enums;

/// <summary>نقش‌های کاربری سیستم (RBAC)</summary>
public enum UserRole
{
    Admin = 1,
    Operator = 2
}

/// <summary>وضعیت عضویت</summary>
public enum MemberStatus
{
    Active = 1,
    Inactive = 2
}

/// <summary>نوع تراکنش سرمایه</summary>
public enum TransactionType
{
    Deposit = 1,      // واریز
    Withdrawal = 2,   // برداشت
    Adjustment = 3    // تعدیل
}

/// <summary>وضعیت قسط</summary>
public enum InstallmentStatus
{
    Future = 1,        // آینده
    Due = 2,           // سررسید شده (امروز)
    PartiallyPaid = 3, // پرداخت جزئی
    Paid = 4,          // پرداخت شده
    Overdue = 5        // معوق
}

/// <summary>وضعیت پرداخت (آبونمان)</summary>
public enum PaymentStatus
{
    Unpaid = 1,
    PartiallyPaid = 2,
    Paid = 3
}
