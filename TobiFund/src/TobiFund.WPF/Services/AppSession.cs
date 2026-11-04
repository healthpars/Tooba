namespace TobiFund.WPF.Services;

/// <summary>وضعیت جلسه برنامه (مثلاً: ورود با رمز پیش‌فرض)</summary>
public static class AppSession
{
    /// <summary>true یعنی کاربر با رمز عبور پیش‌فرض وارد شده و باید آن را تغییر دهد</summary>
    public static bool MustChangePassword { get; set; }
}
