using ClosedXML.Excel;
using FluentAssertions;
using LedKasa.Siparis.Features.Orders;
using LedKasa.Siparis.Features.Orders.Domain;
using LedKasa.Siparis.Features.Reports;
using LedKasa.Siparis.Tests.Infrastructure;

namespace LedKasa.Siparis.Tests.Reports;

public class ReportServiceTests
{
    [Fact]
    public async Task WeeklyReport_ShouldIncludeOnlyMatchingOrders()
    {
        await using var db = TestDb.Create();
        var orders = new OrderService(db, new TestCurrentUser(), new OrderDraftValidator(), new NullTelegramNotifier());
        await orders.CreateAsync(Draft("İçerde", new DateOnly(2026, 9, 10)));
        await orders.CreateAsync(Draft("Dışarıda", new DateOnly(2026, 8, 1)));

        var report = await new ReportService(db).GetAsync(new ReportRequest
        {
            Period = ReportPeriod.Weekly,
            DateField = ReportDateField.OrderDate,
            Anchor = new DateOnly(2026, 9, 10)
        });

        report.OrderCount.Should().Be(1);
        report.GrandTotal.Should().Be(25);
        report.Rows.Single().CustomerName.Should().Be("İçerde");
        report.Rows.Single().GrandTotal.Should().Be(25);
        report.FormattedGrandTotal.Should().Be("25,00");
        report.Title.Should().Contain("Haftalık");
    }

    [Fact]
    public async Task Report_ShouldExcludeCancelledFromRevenue()
    {
        await using var db = TestDb.Create();
        var orders = new OrderService(db, new TestCurrentUser(), new OrderDraftValidator(), new NullTelegramNotifier());
        await orders.CreateAsync(Draft("Aktif", new DateOnly(2026, 9, 10)));
        var cancelledId = await orders.CreateAsync(Draft("İptal", new DateOnly(2026, 9, 10)));
        var cancelled = await orders.GetAsync(cancelledId);
        await orders.ChangeStatusAsync(cancelledId, OrderStatus.Iptal, cancelled!.RowVersion);

        var report = await new ReportService(db).GetAsync(new ReportRequest
        {
            Period = ReportPeriod.Daily,
            Anchor = new DateOnly(2026, 9, 10)
        });

        report.OrderCount.Should().Be(1);
        report.GrandTotal.Should().Be(25);
        report.Rows.Should().ContainSingle(r => r.CustomerName == "Aktif");
        report.Rows.Should().NotContain(r => r.CustomerName == "İptal");
        report.FormattedGrandTotal.Should().Be("25,00");
    }

    [Fact]
    public async Task Report_ShouldListCancelled_WhenFiltered()
    {
        await using var db = TestDb.Create();
        var orders = new OrderService(db, new TestCurrentUser(), new OrderDraftValidator(), new NullTelegramNotifier());
        var cancelledId = await orders.CreateAsync(Draft("İptal", new DateOnly(2026, 9, 10)));
        var cancelled = await orders.GetAsync(cancelledId);
        await orders.ChangeStatusAsync(cancelledId, OrderStatus.Iptal, cancelled!.RowVersion);

        var report = await new ReportService(db).GetAsync(new ReportRequest
        {
            Period = ReportPeriod.Daily,
            Anchor = new DateOnly(2026, 9, 10),
            Statuses = [OrderStatus.Iptal]
        });

        report.Rows.Should().ContainSingle(r => r.CustomerName == "İptal");
    }

    [Fact]
    public async Task Report_ShouldIncludeCancelled_WhenSelectedWithOthers()
    {
        await using var db = TestDb.Create();
        var orders = new OrderService(db, new TestCurrentUser(), new OrderDraftValidator(), new NullTelegramNotifier());
        await orders.CreateAsync(Draft("Aktif", new DateOnly(2026, 9, 10)));
        var cancelledId = await orders.CreateAsync(Draft("İptal", new DateOnly(2026, 9, 10)));
        var cancelled = await orders.GetAsync(cancelledId);
        await orders.ChangeStatusAsync(cancelledId, OrderStatus.Iptal, cancelled!.RowVersion);

        var report = await new ReportService(db).GetAsync(new ReportRequest
        {
            Period = ReportPeriod.Daily,
            Anchor = new DateOnly(2026, 9, 10),
            Statuses = [OrderStatus.Yeni, OrderStatus.Iptal]
        });

        report.OrderCount.Should().Be(2);
        report.GrandTotal.Should().Be(50);
        report.Rows.Select(r => r.CustomerName).Should().BeEquivalentTo(["Aktif", "İptal"]);
    }

    [Fact]
    public async Task ExcelAndPdf_ShouldContainOrderNumber()
    {
        await using var db = TestDb.Create();
        var orders = new OrderService(db, new TestCurrentUser(), new OrderDraftValidator(), new NullTelegramNotifier());
        await orders.CreateAsync(Draft("Rapor", new DateOnly(2026, 9, 10)));
        var report = await new ReportService(db).GetAsync(new ReportRequest
        {
            Period = ReportPeriod.Daily,
            Anchor = new DateOnly(2026, 9, 10)
        });

        var excel = new ExcelReportExporter().Export(report);
        var pdf = new PdfReportExporter().Export(report);

        excel.Should().NotBeEmpty();
        pdf.Should().StartWith("%PDF"u8.ToArray());
        pdf.Length.Should().BeGreaterThan(1000);

        using var book = new XLWorkbook(new MemoryStream(excel));
        var text = book.Worksheet(1).RangeUsed()!.Cells().Select(c => c.GetString()).ToList();
        text.Should().Contain(report.Rows[0].OrderNumber);
        text.Should().Contain("LEDKASA Sipariş Raporu");
        text.Should().Contain("Tutar");
        text.Should().Contain("25,00");
    }

    [Fact]
    public async Task Excel_ShouldFormatGrandTotal()
    {
        await using var db = TestDb.Create();
        var orders = new OrderService(db, new TestCurrentUser(), new OrderDraftValidator(), new NullTelegramNotifier());
        await orders.CreateAsync(Draft("Usd", new DateOnly(2026, 9, 10)));
        var report = await new ReportService(db).GetAsync(new ReportRequest
        {
            Period = ReportPeriod.Daily,
            Anchor = new DateOnly(2026, 9, 10)
        });

        var excel = new ExcelReportExporter().Export(report);
        using var book = new XLWorkbook(new MemoryStream(excel));
        var text = book.Worksheet(1).RangeUsed()!.Cells().Select(c => c.GetString()).ToList();
        text.Should().Contain("25,00");
        report.FormattedGrandTotal.Should().Be("25,00");
    }

    [Fact]
    public void Pdf_ShouldWrapLongCustomerAndMeasures()
    {
        var report = new ReportResult
        {
            Title = "Günlük Liste — 12.09.2026",
            OrderCount = 1,
            ItemCount = 2,
            TotalQuantity = 12,
            FormattedGrandTotal = "1.250,00",
            Rows =
            [
                new ReportRow
                {
                    OrderNumber = "LK-20260912-0001",
                    CustomerName = "Çok Uzun İsimli Müşteri Sanayi ve Ticaret Anonim Şirketi",
                    OrderDate = new DateOnly(2026, 9, 12),
                    DeliveryDate = new DateOnly(2026, 9, 16),
                    DeliveryPlace = "Şirket",
                    Status = "Yeni",
                    ItemCount = 2,
                    TotalQuantity = 12,
                    GrandTotal = 1250,
                    ItemSummary = "Kapaksız LED Kabinet 50x100 cm x4 · CNC LED Kasa 80x120 cm x8 · Rental LED Kabinet 64x48 cm x20"
                }
            ]
        };

        var pdf = new PdfReportExporter().Export(report);
        pdf.Should().StartWith("%PDF"u8.ToArray());
        pdf.Length.Should().BeGreaterThan(1000);
    }

    private static OrderDraft Draft(string name, DateOnly date) => new()
    {
        CustomerName = name,
        OrderDate = date,
        DeliveryDate = date.AddDays(2),
        DeliveryPlace = DeliveryPlace.Sirket,
        Items = [new OrderItemInput { ProductId = 1, WidthCm = 50, HeightCm = 50, Quantity = 1, LineTotal = 25 }]
    };
}
