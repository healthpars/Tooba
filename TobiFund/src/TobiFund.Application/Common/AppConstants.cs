namespace TobiFund.Application.Common;

/// <summary>ثابت‌های کلی برنامه</summary>
public static class AppConstants
{
    public const string AppFolderName = "TobiFund";
    public const string DatabaseFileName = "Fund.db";
    public const string BackupFolderName = "Backup";
    public const string LogFolderName = "Logs";

    /// <summary>کاربر پیش‌فرض که در اولین اجرا ساخته می‌شود</summary>
    public const string DefaultAdminUsername = "admin";
    public const string DefaultAdminPassword = "123456789";

    /// <summary>کد رویدادهای Audit Log</summary>
    public static class AuditActions
    {
        public const string Login = "Login";
        public const string Logout = "Logout";
        public const string MemberCreated = "MemberCreated";
        public const string MemberUpdated = "MemberUpdated";
        public const string MemberDeleted = "MemberDeleted";
        public const string MemberImported = "MemberImported";
        public const string CapitalAdded = "CapitalAdded";
        public const string LoanCreated = "LoanCreated";
        public const string InstallmentPaid = "InstallmentPaid";
        public const string SubscriptionPaid = "SubscriptionPaid";
        public const string SettingsChanged = "SettingsChanged";
        public const string UserCreated = "UserCreated";
        public const string UserUpdated = "UserUpdated";
        public const string PasswordChanged = "PasswordChanged";
        public const string BackupCreated = "BackupCreated";
        public const string BackupRestored = "BackupRestored";
    }
}
