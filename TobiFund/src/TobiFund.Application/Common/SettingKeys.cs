namespace TobiFund.Application.Common;

/// <summary>کلیدهای جدول تنظیمات و مقادیر پیش‌فرض آن‌ها</summary>
public static class SettingKeys
{
    public const string DefaultInstallmentCount = "DefaultInstallmentCount";
    public const string FirstInstallmentDays = "FirstInstallmentDays";
    public const string DefaultSubscriptionAmount = "DefaultSubscriptionAmount";
    public const string CapitalScoreUnit = "CapitalScoreUnit";
    public const string CapitalScoreValue = "CapitalScoreValue";
    public const string LoanScoreUnit = "LoanScoreUnit";
    public const string LoanScoreValue = "LoanScoreValue";

    public static readonly IReadOnlyDictionary<string, string> Defaults = new Dictionary<string, string>
    {
        [DefaultInstallmentCount] = "10",
        [FirstInstallmentDays] = "30",
        [DefaultSubscriptionAmount] = "150000",
        [CapitalScoreUnit] = "1000",
        [CapitalScoreValue] = "1",
        [LoanScoreUnit] = "1000",
        [LoanScoreValue] = "1",
    };

    public static readonly IReadOnlyDictionary<string, string> Descriptions = new Dictionary<string, string>
    {
        [DefaultInstallmentCount] = "تعداد پیش‌فرض اقساط وام",
        [FirstInstallmentDays] = "تعداد روز تا اولین سررسید قسط",
        [DefaultSubscriptionAmount] = "مبلغ پیش‌فرض آبونمان ماهانه",
        [CapitalScoreUnit] = "واحد محاسبه امتیاز سرمایه (به ازای هر X واحد سرمایه)",
        [CapitalScoreValue] = "امتیاز مثبت به ازای هر واحد سرمایه",
        [LoanScoreUnit] = "واحد محاسبه امتیاز وام (به ازای هر X واحد مانده وام)",
        [LoanScoreValue] = "امتیاز منفی به ازای هر واحد مانده وام",
    };
}
