using System.Globalization;
using System.Windows.Data;
using TobiFund.Domain.Enums;

namespace TobiFund.WPF.Converters;

/// <summary>
/// مبدل Enum به متن فارسی.
/// پارامتر: "Installment" | "Subscription" | "Member" | "Transaction" | "Role"
/// </summary>
public class StatusTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is null) return string.Empty;
        var kind = parameter?.ToString();

        return kind switch
        {
            "Installment" when value is InstallmentStatus s => s switch
            {
                InstallmentStatus.Future => "آینده",
                InstallmentStatus.Due => "سررسید",
                InstallmentStatus.PartiallyPaid => "پرداخت جزئی",
                InstallmentStatus.Paid => "پرداخت شده",
                InstallmentStatus.Overdue => "معوق",
                _ => "نامشخص"
            },
            "Subscription" when value is PaymentStatus p => p switch
            {
                PaymentStatus.Unpaid => "پرداخت نشده",
                PaymentStatus.PartiallyPaid => "پرداخت جزئی",
                PaymentStatus.Paid => "پرداخت شده",
                _ => "نامشخص"
            },
            "Member" when value is MemberStatus m => m == MemberStatus.Active ? "فعال" : "غیرفعال",
            "Transaction" when value is TransactionType t => t switch
            {
                TransactionType.Deposit => "واریز",
                TransactionType.Withdrawal => "برداشت",
                TransactionType.Adjustment => "تعدیل",
                _ => "نامشخص"
            },
            "Role" when value is UserRole r => r == UserRole.Admin ? "مدیر" : "اپراتور",
            _ => value.ToString() ?? string.Empty
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        Binding.DoNothing;
}
