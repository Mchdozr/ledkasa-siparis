using LedKasa.Siparis.Common;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LedKasa.Siparis.Features.Reports;

public interface IPdfReportExporter
{
    byte[] Export(ReportResult report);
}

public sealed class PdfReportExporter : IPdfReportExporter
{
    public byte[] Export(ReportResult report)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(28);
                page.DefaultTextStyle(x => x.FontSize(9).FontColor(Colors.BlueGrey.Darken4));

                page.Header().Column(col =>
                {
                    col.Item().Text("LEDKASA").Bold().FontSize(18).FontColor("#0B1F3A");
                    col.Item().Text("Sipariş Raporu").FontSize(12).FontColor("#F46F2C");
                    col.Item().Text(report.Title).SemiBold();
                    col.Item().Text($"Sipariş: {report.OrderCount}  ·  Kalem: {report.ItemCount}  ·  Adet: {report.TotalQuantity}  ·  Tutar: {TurkeyTime.FormatMoney(report.GrandTotal)}");
                    col.Item().PaddingTop(8).LineHorizontal(1).LineColor("#F46F2C");
                });

                page.Content().PaddingTop(12).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(1.2f);
                        columns.RelativeColumn(1.4f);
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(0.8f);
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(0.5f);
                        columns.RelativeColumn(0.5f);
                        columns.RelativeColumn(0.9f);
                        columns.RelativeColumn(1.8f);
                    });

                    table.Header(header =>
                    {
                        foreach (var title in new[] { "Sipariş No", "Sipariş Veren", "Sipariş", "Teslim", "Yer", "Durum", "Kalem", "Adet", "Tutar", "Ölçüler" })
                        {
                            header.Cell().Background("#0B1F3A").Padding(4)
                                .Text(title).FontColor(Colors.White).SemiBold();
                        }
                    });

                    foreach (var row in report.Rows)
                    {
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(row.OrderNumber);
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(row.CustomerName);
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(TurkeyTime.FormatDate(row.OrderDate));
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(TurkeyTime.FormatDate(row.DeliveryDate));
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(row.DeliveryPlace);
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(row.Status);
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(row.ItemCount.ToString());
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(row.TotalQuantity.ToString());
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(TurkeyTime.FormatMoney(row.GrandTotal));
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(row.ItemSummary);
                    }
                });

                page.Footer().AlignRight().Text(text =>
                {
                    text.Span("LEDKASA  ·  siparis.ledkasa.com.tr  ·  ");
                    text.CurrentPageNumber();
                    text.Span(" / ");
                    text.TotalPages();
                });
            });
        }).GeneratePdf();
    }
}
