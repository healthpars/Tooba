using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using ClosedXML.Excel;
using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace TobiFund.WPF.Services;

/// <summary>پیاده‌سازی خروجی Excel (ClosedXML) و PDF (QuestPDF) و Print (WPF)</summary>
public class ExportService : IExportService
{
    public void ExportToExcel(string title, string[] columns, IEnumerable<string[]> rows, string filePath)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("گزارش");

        for (var c = 0; c < columns.Length; c++)
            ws.Cell(1, c + 1).Value = columns[c];
        ws.Row(1).Style.Font.Bold = true;
        ws.Row(1).Style.Font.FontColor = XLColor.White;
        ws.Row(1).Style.Fill.BackgroundColor = XLColor.FromHtml("#2E7D32");

        var r = 2;
        foreach (var row in rows)
        {
            for (var c = 0; c < columns.Length; c++)
                ws.Cell(r, c + 1).Value = c < row.Length ? row[c] : string.Empty;
            r++;
        }

        ws.Columns().AdjustToContents();
        workbook.SaveAs(filePath);
    }

    public void ExportToPdf(string title, string[] columns, IEnumerable<string[]> rows, string filePath)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        var fontFamily = TryRegisterPersianFont();
        var rowList = rows.ToList();

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(24);
                page.DefaultTextStyle(x =>
                {
                    var style = x.FontSize(9);
                    if (fontFamily is not null)
                        style = style.FontFamily(fontFamily);
                    return style;
                });

                page.Header().AlignCenter().Text(title).FontSize(14).Bold();

                page.Content().PaddingTop(10).Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        for (var i = 0; i < columns.Length; i++)
                            cols.RelativeColumn();
                    });

                    table.Header(header =>
                    {
                        foreach (var col in columns)
                        {
                            header.Cell().Background("#2E7D32").Padding(4)
                                .Text(t => t.Span(col).FontColor("#FFFFFF").Bold());
                        }
                    });

                    foreach (var row in rowList)
                    {
                        foreach (var cell in row)
                        {
                            table.Cell().BorderBottom(1).BorderColor("#CCCCCC").Padding(3)
                                .Text(t => t.Span(cell ?? string.Empty));
                        }
                    }
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("صفحه ");
                    x.CurrentPageNumber();
                    x.Span(" از ");
                    x.TotalPages();
                });
            });
        }).GeneratePdf(filePath);
    }

    public void Print(string title, string[] columns, IEnumerable<string[]> rows)
    {
        var doc = new FlowDocument
        {
            FlowDirection = FlowDirection.RightToLeft,
            FontFamily = new FontFamily("Tahoma"),
            FontSize = 11,
            PagePadding = new Thickness(40)
        };

        doc.Blocks.Add(new Paragraph(new Run(title))
        {
            FontSize = 16,
            FontWeight = FontWeights.Bold,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 15)
        });

        var table = new Table { CellSpacing = 0 };
        foreach (var _ in columns)
            table.Columns.Add(new TableColumn());

        var headerGroup = new TableRowGroup();
        var headerRow = new TableRow();
        foreach (var col in columns)
        {
            headerRow.Cells.Add(new TableCell(new Paragraph(new Run(col)))
            {
                Background = new SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#2E7D32")!),
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                Padding = new Thickness(5)
            });
        }
        headerGroup.Rows.Add(headerRow);
        table.RowGroups.Add(headerGroup);

        var bodyGroup = new TableRowGroup();
        foreach (var row in rows)
        {
            var tableRow = new TableRow();
            foreach (var cell in row)
                tableRow.Cells.Add(new TableCell(new Paragraph(new Run(cell ?? string.Empty)))
                {
                    Padding = new Thickness(5)
                });
            bodyGroup.Rows.Add(tableRow);
        }
        table.RowGroups.Add(bodyGroup);
        doc.Blocks.Add(table);

        var printDialog = new PrintDialog();
        if (printDialog.ShowDialog() == true)
            printDialog.PrintDocument(((IDocumentPaginatorSource)doc).DocumentPaginator, title);
    }

    /// <summary>ثبت فونت فارسی از پوشه فونت‌های ویندوز (Tahoma / Segoe UI)</summary>
    private static string? TryRegisterPersianFont()
    {
        try
        {
            var fontsDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Fonts");
            foreach (var fileName in new[] { "tahoma.ttf", "segoeui.ttf" })
            {
                var path = Path.Combine(fontsDir, fileName);
                if (!File.Exists(path)) continue;
                using var stream = File.OpenRead(path);
                QuestPDF.Drawing.FontManager.RegisterFont(stream);
                return fileName == "tahoma.ttf" ? "Tahoma" : "Segoe UI";
            }
        }
        catch
        {
            // در صورت عدم موفقیت، از فونت پیش‌فرض QuestPDF استفاده می‌شود
        }
        return null;
    }
}
