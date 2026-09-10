using LedKasa.Siparis.Common;
using PdfSharpCore.Drawing;
using PdfSharpCore.Fonts;
using PdfSharpCore.Pdf;

namespace LedKasa.Siparis.Features.Reports;

public interface IPdfReportExporter
{
    byte[] Export(ReportResult report);
}

public sealed class PdfReportExporter : IPdfReportExporter
{
    private static readonly XSolidBrush NavyBrush = new(XColor.FromArgb(0x0B, 0x1F, 0x3A));
    private static readonly XSolidBrush OrangeBrush = new(XColor.FromArgb(0xF4, 0x6F, 0x2C));
    private static readonly XSolidBrush WhiteBrush = new(XColors.White);
    private static readonly XSolidBrush TextBrush = new(XColor.FromArgb(0x26, 0x32, 0x38));
    private static readonly XPen RowLine = new(XColor.FromArgb(0xE0, 0xE0, 0xE0), 0.5);
    private static readonly XPen AccentLine = new(XColor.FromArgb(0xF4, 0x6F, 0x2C), 1);

    private static readonly float[] ColumnWeights = [1.2f, 1.4f, 1f, 1f, 0.8f, 1f, 0.5f, 0.5f, 0.9f, 1.8f];
    private static readonly string[] Headers =
    [
        "Sipariş No", "Sipariş Veren", "Sipariş", "Teslim", "Yer",
        "Durum", "Kalem", "Adet", "Tutar", "Ölçüler"
    ];

    static PdfReportExporter()
    {
        GlobalFontSettings.FontResolver = ReportFontResolver.Instance;
        GlobalFontSettings.DefaultFontEncoding = PdfFontEncoding.Unicode;
    }

    public byte[] Export(ReportResult report)
    {
        using var document = new PdfDocument();
        document.Info.Title = "LEDKASA Sipariş Raporu";

        var titleFont = new XFont(ReportFontResolver.Family, 16, XFontStyle.Bold);
        var subtitleFont = new XFont(ReportFontResolver.Family, 11, XFontStyle.Regular);
        var metaFont = new XFont(ReportFontResolver.Family, 9, XFontStyle.Bold);
        var headerFont = new XFont(ReportFontResolver.Family, 8, XFontStyle.Bold);
        var cellFont = new XFont(ReportFontResolver.Family, 8, XFontStyle.Regular);
        var footerFont = new XFont(ReportFontResolver.Family, 8, XFontStyle.Regular);

        const double margin = 28;
        const double headerHeight = 18;
        const double rowHeight = 16;

        PdfPage? page = null;
        XGraphics? gfx = null;
        double y = 0;
        double[] xs = [];
        double[] widths = [];

        void NewPage()
        {
            gfx?.Dispose();
            page = document.AddPage();
            page.Size = PdfSharpCore.PageSize.A4;
            page.Orientation = PdfSharpCore.PageOrientation.Landscape;
            gfx = XGraphics.FromPdfPage(page);
            y = margin;

            gfx.DrawString("LEDKASA", titleFont, NavyBrush, new XPoint(margin, y));
            y += 18;
            gfx.DrawString("Sipariş Raporu", subtitleFont, OrangeBrush, new XPoint(margin, y));
            y += 14;
            gfx.DrawString(report.Title, metaFont, TextBrush, new XPoint(margin, y));
            y += 13;
            gfx.DrawString(
                $"Sipariş: {report.OrderCount}  ·  Kalem: {report.ItemCount}  ·  Adet: {report.TotalQuantity}  ·  Tutar: {report.FormattedGrandTotal}",
                cellFont, TextBrush, new XPoint(margin, y));
            y += 8;
            gfx.DrawLine(AccentLine, margin, y, page.Width - margin, y);
            y += 10;

            (xs, widths) = BuildColumns(page.Width - margin * 2, margin);
            gfx.DrawRectangle(NavyBrush, xs[0], y, widths.Sum(), headerHeight);
            for (var i = 0; i < Headers.Length; i++)
            {
                gfx.DrawString(Headers[i], headerFont, WhiteBrush,
                    new XRect(xs[i] + 2, y, widths[i] - 4, headerHeight),
                    new XStringFormat { Alignment = XStringAlignment.Near, LineAlignment = XLineAlignment.Center });
            }

            y += headerHeight;
        }

        NewPage();

        foreach (var row in report.Rows)
        {
            if (y + rowHeight > page!.Height - margin - 16)
                NewPage();

            var values = new[]
            {
                row.OrderNumber,
                row.CustomerName,
                TurkeyTime.FormatDate(row.OrderDate),
                TurkeyTime.FormatDate(row.DeliveryDate),
                row.DeliveryPlace,
                row.Status,
                row.ItemCount.ToString(),
                row.TotalQuantity.ToString(),
                TurkeyTime.FormatMoney(row.GrandTotal, row.Currency),
                row.ItemSummary
            };

            for (var i = 0; i < values.Length; i++)
            {
                gfx!.DrawString(values[i], cellFont, TextBrush,
                    new XRect(xs[i] + 2, y, widths[i] - 4, rowHeight),
                    new XStringFormat { Alignment = XStringAlignment.Near, LineAlignment = XLineAlignment.Center });
            }

            gfx!.DrawLine(RowLine, margin, y + rowHeight, page.Width - margin, y + rowHeight);
            y += rowHeight;
        }

        gfx?.Dispose();

        for (var i = 1; i <= document.PageCount; i++)
        {
            using var footerGfx = XGraphics.FromPdfPage(document.Pages[i - 1], XGraphicsPdfPageOptions.Append);
            footerGfx.DrawString(
                $"LEDKASA  ·  siparis.ledkasa.com.tr  ·  {i} / {document.PageCount}",
                footerFont, TextBrush,
                new XRect(margin, document.Pages[i - 1].Height - margin, document.Pages[i - 1].Width - margin * 2, 12),
                XStringFormats.CenterRight);
        }

        using var stream = new MemoryStream();
        document.Save(stream, false);
        return stream.ToArray();
    }

    private static (double[] xs, double[] widths) BuildColumns(double tableWidth, double margin)
    {
        var total = ColumnWeights.Sum();
        var xs = new double[ColumnWeights.Length];
        var widths = new double[ColumnWeights.Length];
        var x = margin;
        for (var i = 0; i < ColumnWeights.Length; i++)
        {
            widths[i] = tableWidth * (ColumnWeights[i] / total);
            xs[i] = x;
            x += widths[i];
        }

        return (xs, widths);
    }
}
