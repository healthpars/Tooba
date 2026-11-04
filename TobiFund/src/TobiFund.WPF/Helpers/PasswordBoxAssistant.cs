using System.Windows;
using System.Windows.Controls;

namespace TobiFund.WPF.Helpers;

/// <summary>
/// Bindable Password برای PasswordBox (امکان Binding مستقیم).
/// مقدار پیش‌فرض null است تا PropertyChangedCallback هنگام اتصال اولیه Binding حتماً اجرا شود
/// و رویداد PasswordChanged حتماً subscribe شود؛ اگر مقدار پیش‌فرض "" باشد و مقدار اولیه
/// ViewModel هم "" باشد، callback اجرا نمی‌شود و تایپ در PasswordBox به ViewModel منتقل نمی‌شود.
/// </summary>
public static class PasswordBoxAssistant
{
    public static readonly DependencyProperty BoundPasswordProperty =
        DependencyProperty.RegisterAttached(
            "BoundPassword",
            typeof(string),
            typeof(PasswordBoxAssistant),
            new FrameworkPropertyMetadata(null,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnBoundPasswordChanged));

    public static string GetBoundPassword(DependencyObject d) =>
        (string?)d.GetValue(BoundPasswordProperty) ?? string.Empty;

    public static void SetBoundPassword(DependencyObject d, string value) =>
        d.SetValue(BoundPasswordProperty, value);

    private static void OnBoundPasswordChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not PasswordBox box) return;
        box.PasswordChanged -= Box_PasswordChanged;
        if (!Equals(e.NewValue, box.Password))
            box.Password = e.NewValue as string ?? string.Empty;
        box.PasswordChanged += Box_PasswordChanged;
    }

    private static void Box_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (sender is PasswordBox box)
            SetBoundPassword(box, box.Password);
    }
}
