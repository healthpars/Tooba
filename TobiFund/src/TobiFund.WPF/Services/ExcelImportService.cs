using System.Globalization;
using System.IO;
using ClosedXML.Excel;
using TobiFund.Application.Common;
using TobiFund.Application.DTOs;

namespace TobiFund.WPF.Services;

/// <summary>
/// Reads an Excel file of members. Expected columns (Persian headers):
/// شناسه | نام | نام خانوادگی | سرمایه | وام
/// Headers are matched by name; if not found, columns are read positionally (1..5).
/// </summary>
public class ExcelImportService : IExcelImportService
{
    public IReadOnlyList<ImportMemberRowDto> ReadMembers(string filePath)
    {
        using var workbook = new XLWorkbook(filePath);
        var ws = workbook.Worksheets.First();
        if (ws.RowsUsed() is null)
            return new List<ImportMemberRowDto>();

        var map = BuildHeaderMap(ws);
        var firstDataRow = map.MatchedCount >= 2 ? 2 : 1;

        var result = new List<ImportMemberRowDto>();
        var allRows = ws.RowsUsed()!.ToList();
        for (var i = firstDataRow - 1; i < allRows.Count; i++)
        {
            var row = allRows[i];
            var rowNumber = i + 1;

            var dto = new ImportMemberRowDto
            {
                MemberCode = GetCellText(row, map.CodeColumn),
                FirstName = GetCellText(row, map.FirstNameColumn),
                LastName = GetCellText(row, map.LastNameColumn),
                Capital = ParseAmount(GetCellText(row, map.CapitalColumn), rowNumber, "سرمایه"),
                Loan = ParseAmount(GetCellText(row, map.LoanColumn), rowNumber, "وام")
            };

            var isEmpty = string.IsNullOrWhiteSpace(dto.MemberCode)
                          && string.IsNullOrWhiteSpace(dto.FirstName)
                          && string.IsNullOrWhiteSpace(dto.LastName)
                          && dto.Capital == 0m
                          && dto.Loan == 0m;
            if (isEmpty) continue;

            result.Add(dto);
        }
        return result;
    }

    private sealed record ColumnMap(
        int CodeColumn, int FirstNameColumn, int LastNameColumn, int CapitalColumn, int LoanColumn, int MatchedCount);

    /// <summary>Single pass over the header row. More specific aliases are checked first so that
    /// «نام خانوادگی» is not mistaken for «نام».</summary>
    private static ColumnMap BuildHeaderMap(IXLWorksheet ws)
    {
        var code = 1; var first = 2; var last = 3; var capital = 4; var loan = 5;
        var matched = 0;
        var lastCol = ws.LastColumnUsed()?.ColumnNumber() ?? 0;
        for (var c = 1; c <= lastCol; c++)
        {
            var h = Normalize(ws.Cell(1, c).GetString());
            if (h.Length == 0) continue;

            if (h.Contains("خانوادگی") || h.Contains("فامیل") || h.Contains("lastname")) { last = c; matched++; }
            else if (h.Contains("شناسه") || h.Contains("کد") || h.Contains("code") || h.Contains("id") || h.Contains("membercode")) { code = c; matched++; }
            else if (h.Contains("سرمایه") || h.Contains("capital")) { capital = c; matched++; }
            else if (h.Contains("وام") || h.Contains("loan")) { loan = c; matched++; }
            else if (h.Contains("نام") || h.Contains("firstname")) { first = c; matched++; }
        }
        return new ColumnMap(code, first, last, capital, loan, matched);
    }

    private static string Normalize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        return text.Trim()
            .Replace(" ", "")
            .Replace("‌", "") // ZWNJ
            .Replace("_", "")
            .ToLowerInvariant();
    }

    private static string GetCellText(IXLRow row, int column)
    {
        if (column <= 0) return string.Empty;
        return row.Cell(column).GetString()?.Trim() ?? string.Empty;
    }

    private static decimal ParseAmount(string text, int rowNumber, string columnName)
    {
        if (string.IsNullOrWhiteSpace(text)) return 0m;
        if (PersianDateHelper.TryParseAmount(text, out var amount))
            return amount;
        if (decimal.TryParse(text.Replace(",", ""), NumberStyles.Number, CultureInfo.InvariantCulture, out var direct))
            return direct;
        throw new InvalidDataException(
            $"مبلغ «{columnName}» در سطر {PersianDateHelper.ToPersianDigits(rowNumber.ToString())} نامعتبر است: {text}");
    }
}
