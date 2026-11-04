using TobiFund.Application.Common;
using TobiFund.Application.Interfaces;

namespace TobiFund.Infrastructure.Services;

/// <summary>لاگر فایل — لاگ‌ها در %AppData%/TobiFund/Logs ذخیره می‌شوند</summary>
public class FileLogger : IAppLogger
{
    private readonly string _logDirectory;
    private readonly object _lock = new();

    public FileLogger() : this(GetDefaultLogDirectory())
    {
    }

    public FileLogger(string logDirectory)
    {
        _logDirectory = logDirectory;
        Directory.CreateDirectory(_logDirectory);
    }

    public static string GetDefaultLogDirectory()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, AppConstants.AppFolderName, AppConstants.LogFolderName);
    }

    public void Info(string message) => Write("INFO", message, null);

    public void Warning(string message) => Write("WARN", message, null);

    public void Error(string message, Exception? exception = null) => Write("ERROR", message, exception);

    private void Write(string level, string message, Exception? exception)
    {
        try
        {
            var file = Path.Combine(_logDirectory, $"app-{DateTime.Now:yyyyMMdd}.log");
            var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}";
            if (exception is not null)
                line += $" | Exception: {exception.GetType().Name}: {exception.Message}";
            lock (_lock)
            {
                File.AppendAllText(file, line + Environment.NewLine);
            }
        }
        catch
        {
            // لاگینگ هرگز نباید برنامه را متوقف کند
        }
    }
}
