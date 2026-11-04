using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TobiFund.Application.Common;
using TobiFund.Application.Interfaces;

namespace TobiFund.Application.Services;

/// <summary>پشتیبان‌گیری و بازیابی دیتابیس SQLite (کاملاً آفلاین)</summary>
public class BackupService : IBackupService
{
    private readonly IApplicationDbContext _context;
    private readonly IAuditService _audit;
    private readonly IAppLogger _logger;

    public BackupService(IApplicationDbContext context, IAuditService audit, IAppLogger logger)
        : this(context, audit, logger, GetDefaultDatabasePath())
    {
    }

    public BackupService(IApplicationDbContext context, IAuditService audit, IAppLogger logger, string databasePath)
    {
        _context = context;
        _audit = audit;
        _logger = logger;
        DatabasePath = databasePath;
        BackupDirectory = Path.Combine(Path.GetDirectoryName(databasePath)!, "..", AppConstants.BackupFolderName);
        BackupDirectory = Path.GetFullPath(BackupDirectory);
    }

    public string DatabasePath { get; }
    public string BackupDirectory { get; }

    public static string GetDefaultDatabasePath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, AppConstants.AppFolderName, "Data", AppConstants.DatabaseFileName);
    }

    public async Task<ServiceResult<string>> BackupAsync()
    {
        try
        {
            if (!File.Exists(DatabasePath))
                return ServiceResult<string>.Fail("فایل دیتابیس یافت نشد.");

            Directory.CreateDirectory(BackupDirectory);

            // ژورنال WAL را قبل از کپی، تخلیه (Checkpoint) می‌کنیم
            try
            {
                var db = (Microsoft.EntityFrameworkCore.DbContext)_context;
                await db.Database.ExecuteSqlRawAsync("PRAGMA wal_checkpoint(TRUNCATE);");
            }
            catch (Exception ex)
            {
                _logger.Warning($"wal_checkpoint انجام نشد: {ex.Message}");
            }

            var fileName = $"Fund_{DateTime.Now:yyyy-MM-dd_HH-mm}.db";
            var destination = Path.Combine(BackupDirectory, fileName);
            if (File.Exists(destination))
                destination = Path.Combine(BackupDirectory, $"Fund_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.db");

            SqliteConnection.ClearAllPools();
            File.Copy(DatabasePath, destination, overwrite: false);

            // کپی فایل‌های WAL/SHM (در صورت وجود)
            foreach (var suffix in new[] { "-wal", "-shm" })
            {
                var sourceSide = DatabasePath + suffix;
                if (File.Exists(sourceSide) && new FileInfo(sourceSide).Length > 0)
                    File.Copy(sourceSide, destination + suffix, overwrite: true);
            }

            await _audit.LogAsync(AppConstants.AuditActions.BackupCreated, "Database", destination,
                $"پشتیبان‌گیری از دیتابیس ایجاد شد: {Path.GetFileName(destination)}");

            _logger.Info($"Backup ایجاد شد: {destination}");
            return ServiceResult<string>.Ok(destination, $"پشتیبان‌گیری با موفقیت ایجاد شد:{Environment.NewLine}{destination}");
        }
        catch (Exception ex)
        {
            _logger.Error("خطا در پشتیبان‌گیری", ex);
            return ServiceResult<string>.Fail("خطا در پشتیبان‌گیری: " + ex.Message);
        }
    }

    public async Task<ServiceResult> RestoreAsync(string backupFilePath)
    {
        try
        {
            if (!File.Exists(backupFilePath))
                return ServiceResult.Fail("فایل پشتیبان یافت نشد.");
            if (!backupFilePath.EndsWith(".db", StringComparison.OrdinalIgnoreCase))
                return ServiceResult.Fail("فایل انتخاب‌شده معتبر نیست. (باید .db باشد)");

            SqliteConnection.ClearAllPools();

            var directory = Path.GetDirectoryName(DatabasePath)!;
            Directory.CreateDirectory(directory);

            File.Copy(backupFilePath, DatabasePath, overwrite: true);

            // پاک کردن فایل‌های WAL/SHM قدیمی
            foreach (var suffix in new[] { "-wal", "-shm" })
            {
                var side = DatabasePath + suffix;
                if (File.Exists(side)) File.Delete(side);
            }

            await _audit.LogAsync(AppConstants.AuditActions.BackupRestored, "Database", backupFilePath,
                $"بازیابی دیتابیس از فایل {Path.GetFileName(backupFilePath)} انجام شد.");

            _logger.Info($"Restore از فایل {backupFilePath} انجام شد.");
            return ServiceResult.Ok("بازیابی با موفقیت انجام شد. لطفاً برنامه را ببندید و دوباره اجرا کنید.");
        }
        catch (Exception ex)
        {
            _logger.Error("خطا در بازیابی", ex);
            return ServiceResult.Fail("خطا در بازیابی: " + ex.Message);
        }
    }

    public IReadOnlyList<string> GetBackupFiles()
    {
        if (!Directory.Exists(BackupDirectory))
            return Array.Empty<string>();
        return Directory.GetFiles(BackupDirectory, "*.db")
            .OrderByDescending(f => f)
            .ToList();
    }
}
