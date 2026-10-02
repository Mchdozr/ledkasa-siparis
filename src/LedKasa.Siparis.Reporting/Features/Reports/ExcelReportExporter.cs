using ClosedXML.Excel;
using LedKasa.Siparis.Common;

namespace LedKasa.Siparis.Features.Reports;

public interface IExcelReportExporter
{
    byte[] Export(ReportResult report);
}

internal sealed class ExcelReportExporter : IExcelReportExporter
{
    private const int NotesColumn = 10;

    public byte[] Export(ReportResult report)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Sipariş Listesi");

        sheet.Cell(1, 1).Value = "LEDKASA Sipariş Raporu";
        sheet.Cell(1, 1).Style.Font.Bold = true;
        sheet.Cell(1, 1).Style.Font.FontSize = 16;
        var lastColumn = report.IncludeNotes ? NotesColumn : 9;
        sheet.Range(1, 1, 1, lastColumn).Merge();

        sheet.Cell(2, 1).Value = report.Title;
        sheet.Range(2, 1, 2, lastColumn).Merge();
        sheet.Cell(3, 1).Value = $"Sipariş: {report.OrderCount}  |  Kalem: {report.ItemCount}  |  Adet: {report.TotalQuantity}";
        sheet.Range(3, 1, 3, lastColumn).Merge();

        string[] headers = ["Sipariş No", "Sipariş Veren", "Sipariş Tarihi", "Teslim Tarihi", "Teslim Yeri", "Durum", "Kalem", "Toplam Adet", "Ölçüler"];
        if (report.IncludeNotes)
            headers = [.. headers, "Notlar"];
        for (var i = 0; i < headers.Length; i++)
        {
            var cell = sheet.Cell(5, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#0B1F3A");
            cell.Style.Font.FontColor = XLColor.White;
        }

        var row = 6;
        foreach (var item in report.Rows)
        {
            sheet.Cell(row, 1).Value = item.OrderNumber;
            sheet.Cell(row, 2).Value = item.CustomerName;
            sheet.Cell(row, 3).Value = TurkeyTime.FormatDate(item.OrderDate);
            sheet.Cell(row, 4).Value = TurkeyTime.FormatDate(item.DeliveryDate);
            sheet.Cell(row, 5).Value = item.DeliveryPlace;
            sheet.Cell(row, 6).Value = item.Status;
            sheet.Cell(row, 7).Value = item.ItemCount;
            sheet.Cell(row, 8).Value = item.TotalQuantity;
            sheet.Cell(row, 9).Value = item.ItemSummary;
            if (report.IncludeNotes)
            {
                var notes = sheet.Cell(row, NotesColumn);
                notes.Value = item.Notes ?? string.Empty;
                notes.Style.Alignment.WrapText = true;
            }
            row++;
        }

        sheet.Columns().AdjustToContents();
        if (report.IncludeNotes)
        {
            sheet.Column(NotesColumn).Width = 60;
            if (row > 6)
                sheet.Range(6, 1, row - 1, NotesColumn).Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
        }
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
