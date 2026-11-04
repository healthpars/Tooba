using System.Windows;

namespace TobiFund.WPF.Services;

/// <summary>دیالوگ‌های پیام (متن فارسی)</summary>
public static class DialogService
{
    public static void ShowInfo(string message, string title = "صندوق طوبی") =>
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);

    public static void ShowError(string message, string title = "خطا") =>
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);

    public static void ShowWarning(string message, string title = "هشدار") =>
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);

    public static bool Confirm(string message, string title = "تأیید") =>
        MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question,
            MessageBoxResult.No) == MessageBoxResult.Yes;
}
