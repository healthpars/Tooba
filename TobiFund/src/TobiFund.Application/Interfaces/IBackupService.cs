using TobiFund.Application.Common;

namespace TobiFund.Application.Interfaces;

public interface IBackupService
{
    string DatabasePath { get; }
    string BackupDirectory { get; }

    /// <summary>کپی گرفتن از دیتابیس در پوشه Backup (نام فایل: Fund_yyyy-MM-dd_HH-mm.db)</summary>
    Task<ServiceResult<string>> BackupAsync();

    /// <summary>بازیابی دیتابیس از فایل پشتیبان (نیاز به راه‌اندازی مجدد برنامه دارد)</summary>
    Task<ServiceResult> RestoreAsync(string backupFilePath);

    IReadOnlyList<string> GetBackupFiles();
}
