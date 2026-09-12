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

    private static readonly float[] ColumnWeights = [1.15f, 1.7f, 0.85f, 0.85f, 0.7f, 0.85f, 0.45f, 0.45f, 0.85f, 2.3f];
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
        const double lineHeight = 10.5;
        const double rowPad = 3;

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

            var wrapped = values
                .Select((value, i) => PdfCellText.Wrap(gfx!, value, cellFont, widths[i] - 4))
                .ToArray();
            var thisRowHeight = Math.Max(16, wrapped.Max(lines => lines.Count) * lineHeight + rowPad * 2);

            if (y + thisRowHeight > page!.Height - margin - 16)
                NewPage();

            for (var i = 0; i < wrapped.Length; i++)
            {
                var state = gfx!.Save();
                gfx.IntersectClip(new XRect(xs[i], y, widths[i], thisRowHeight));
                for (var line = 0; line < wrapped[i].Count; line++)
                {
                    gfx.DrawString(
                        wrapped[i][line],
                        cellFont,
                        TextBrush,
                        new XRect(xs[i] + 2, y + rowPad + line * lineHeight, widths[i] - 4, lineHeight),
                        new XStringFormat { Alignment = XStringAlignment.Near, LineAlignment = XLineAlignment.Center });
                }

                gfx.Restore(state);
            }

            gfx!.DrawLine(RowLine, margin, y + thisRowHeight, page.Width - margin, y + thisRowHeight);
            y += thisRowHeight;
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

internal static class PdfCellText
{
    public static IReadOnlyList<string> Wrap(XGraphics gfx, string text, XFont font, double maxWidth)
    {
        if (string.IsNullOrWhiteSpace(text))
            return [string.Empty];
        if (maxWidth <= 4 || gfx.MeasureString(text, font).Width <= maxWidth)
            return [text];

        var lines = new List<string>();
        var current = string.Empty;
        foreach (var word in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var candidate = current.Length == 0 ? word : current + " " + word;
            if (gfx.MeasureString(candidate, font).Width <= maxWidth)
            {
                current = candidate;
                continue;
            }

            if (current.Length > 0)
                lines.Add(current);

            if (gfx.MeasureString(word, font).Width <= maxWidth)
            {
                current = word;
                continue;
            }

            current = string.Empty;
            foreach (var ch in word)
            {
                var next = current + ch;
                if (gfx.MeasureString(next, font).Width <= maxWidth || current.Length == 0)
                {
                    current = next;
                    continue;
                }

                lines.Add(current);
                current = ch.ToString();
            }
        }

        if (current.Length > 0)
            lines.Add(current);

        return lines.Count == 0 ? [string.Empty] : lines;
    }
}
