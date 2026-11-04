namespace TobiFund.Application.Interfaces;

/// <summary>لاگر برنامه (پیاده‌سازی فایل‌لاگر در Infrastructure)</summary>
public interface IAppLogger
{
    void Info(string message);
    void Warning(string message);
    void Error(string message, Exception? exception = null);
}
