using ClosedXML.Excel;
using LedKasa.Siparis.Common;

namespace LedKasa.Siparis.Features.Reports;

public interface IExcelReportExporter
{
    byte[] Export(ReportResult report);
}

public sealed class ExcelReportExporter : IExcelReportExporter
{
    public byte[] Export(ReportResult report)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Sipariş Listesi");

        sheet.Cell(1, 1).Value = "LEDKASA Sipariş Raporu";
        sheet.Cell(1, 1).Style.Font.Bold = true;
        sheet.Cell(1, 1).Style.Font.FontSize = 16;
        sheet.Range(1, 1, 1, 10).Merge();

        sheet.Cell(2, 1).Value = report.Title;
        sheet.Range(2, 1, 2, 10).Merge();
        sheet.Cell(3, 1).Value = $"Sipariş: {report.OrderCount}  |  Kalem: {report.ItemCount}  |  Adet: {report.TotalQuantity}  |  Tutar: {report.FormattedGrandTotal}";
        sheet.Range(3, 1, 3, 10).Merge();

        var headers = new[] { "Sipariş No", "Sipariş Veren", "Sipariş Tarihi", "Teslim Tarihi", "Teslim Yeri", "Durum", "Kalem", "Toplam Adet", "Tutar", "Ölçüler" };
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
            sheet.Cell(row, 9).Value = TurkeyTime.FormatMoney(item.GrandTotal, item.Currency);
            sheet.Cell(row, 10).Value = item.ItemSummary;
            row++;
        }

        sheet.Columns().AdjustToContents();
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
