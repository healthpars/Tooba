namespace TobiFund.WPF.Services;

/// <summary>خروجی گزارش‌ها: Excel / PDF / Print</summary>
public interface IExportService
{
    void ExportToExcel(string title, string[] columns, IEnumerable<string[]> rows, string filePath);
    void ExportToPdf(string title, string[] columns, IEnumerable<string[]> rows, string filePath);
    void Print(string title, string[] columns, IEnumerable<string[]> rows);
}
