using ClosedXML.Excel;
using FluentAssertions;
using LedKasa.Siparis.Features.Orders;
using LedKasa.Siparis.Features.Orders.Domain;
using LedKasa.Siparis.Features.Reports;
using LedKasa.Siparis.Tests.Infrastructure;
using PdfSharpCore.Pdf.IO;

namespace LedKasa.Siparis.Tests.Reports;

public class ReportServiceTests
{
    [Fact]
    public async Task WeeklyReport_ShouldIncludeOnlyMatchingOrders()
    {
        await using var store = TestDb.CreateStore();
        var orders = new OrderService(store.Orders, new TestCurrentUser(), new OrderDraftValidator());
        await orders.CreateAsync(Draft("İçerde", new DateOnly(2026, 9, 10)));
        await orders.CreateAsync(Draft("Dışarıda", new DateOnly(2026, 8, 1)));

        var report = await new ReportService(store.Reporting).GetAsync(new ReportRequest
        {
            Period = ReportPeriod.Weekly,
            DateField = ReportDateField.OrderDate,
            Anchor = new DateOnly(2026, 9, 10)
        });

        report.OrderCount.Should().Be(1);
        report.Rows.Single().CustomerName.Should().Be("İçerde");
        report.Rows.Single().ItemSummary.Should().Contain("50x50x5 cm Tek yön x1");
        report.Title.Should().Contain("Haftalık");
    }

    [Fact]
    public async Task Report_ShouldExcludeCancelledByDefault()
    {
        await using var store = TestDb.CreateStore();
        var orders = new OrderService(store.Orders, new TestCurrentUser(), new OrderDraftValidator());
        await orders.CreateAsync(Draft("Aktif", new DateOnly(2026, 9, 10)));
        var cancelledId = await orders.CreateAsync(Draft("İptal", new DateOnly(2026, 9, 10)));
        var cancelled = await orders.GetAsync(cancelledId);
        await orders.ChangeStatusAsync(cancelledId, OrderStatus.Iptal, cancelled!.RowVersion);

        var report = await new ReportService(store.Reporting).GetAsync(new ReportRequest
        {
            Period = ReportPeriod.Daily,
            Anchor = new DateOnly(2026, 9, 10)
        });

        report.OrderCount.Should().Be(1);
        report.Rows.Should().ContainSingle(r => r.CustomerName == "Aktif");
        report.Rows.Should().NotContain(r => r.CustomerName == "İptal");
    }

    [Fact]
    public async Task Report_ShouldListCancelled_WhenFiltered()
    {
        await using var store = TestDb.CreateStore();
        var orders = new OrderService(store.Orders, new TestCurrentUser(), new OrderDraftValidator());
        var cancelledId = await orders.CreateAsync(Draft("İptal", new DateOnly(2026, 9, 10)));
        var cancelled = await orders.GetAsync(cancelledId);
        await orders.ChangeStatusAsync(cancelledId, OrderStatus.Iptal, cancelled!.RowVersion);

        var report = await new ReportService(store.Reporting).GetAsync(new ReportRequest
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
        await using var store = TestDb.CreateStore();
        var orders = new OrderService(store.Orders, new TestCurrentUser(), new OrderDraftValidator());
        await orders.CreateAsync(Draft("Aktif", new DateOnly(2026, 9, 10)));
        var cancelledId = await orders.CreateAsync(Draft("İptal", new DateOnly(2026, 9, 10)));
        var cancelled = await orders.GetAsync(cancelledId);
        await orders.ChangeStatusAsync(cancelledId, OrderStatus.Iptal, cancelled!.RowVersion);

        var report = await new ReportService(store.Reporting).GetAsync(new ReportRequest
        {
            Period = ReportPeriod.Daily,
            Anchor = new DateOnly(2026, 9, 10),
            Statuses = [OrderStatus.Yeni, OrderStatus.Iptal]
        });

        report.OrderCount.Should().Be(2);
        report.Rows.Select(r => r.CustomerName).Should().BeEquivalentTo(["Aktif", "İptal"]);
    }

    [Fact]
    public async Task ExcelAndPdf_ShouldContainOrderNumber()
    {
        await using var store = TestDb.CreateStore();
        var orders = new OrderService(store.Orders, new TestCurrentUser(), new OrderDraftValidator());
        await orders.CreateAsync(Draft("Rapor", new DateOnly(2026, 9, 10)));
        var report = await new ReportService(store.Reporting).GetAsync(new ReportRequest
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
        text.Should().NotContain("Tutar");
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
                    ItemSummary = "Kapaksız LED Kabinet 50x100 cm x4 · CNC LED Kasa 80x120 cm x8 · Rental LED Kabinet 64x48 cm x20"
                }
            ]
        };

        var pdf = new PdfReportExporter().Export(report);
        pdf.Should().StartWith("%PDF"u8.ToArray());
        pdf.Length.Should().BeGreaterThan(1000);
    }

    [Fact]
    public async Task OrderListExport_ShouldMatchListFiltersAndSort()
    {
        await using var store = TestDb.CreateStore();
        var orders = new OrderService(store.Orders, new TestCurrentUser(), new OrderDraftValidator());
        for (var i = 0; i < 3; i++)
            await orders.CreateAsync(Draft($"Ayşe {i}", new DateOnly(2026, 9, 10 + i)));
        var fabrika = Draft("Ayşe Fabrika", new DateOnly(2026, 9, 12));
        fabrika.DeliveryPlace = DeliveryPlace.Fabrika;
        await orders.CreateAsync(fabrika);
        await orders.CreateAsync(Draft("Mehmet", new DateOnly(2026, 9, 12)));
        await orders.CreateAsync(Draft("Ayşe Eski", new DateOnly(2026, 8, 1)));

        var filter = new OrderListFilter
        {
            Search = "Ayşe",
            DeliveryPlace = DeliveryPlace.Sirket,
            From = new DateOnly(2026, 9, 10),
            Sort = OrderSort.OldestFirst,
            PageSize = 5
        };
        var list = await orders.ListAsync(filter);
        var report = await new ReportService(store.Reporting).GetForOrderListAsync(filter);

        report.Rows.Select(r => r.OrderNumber).Should().Equal(list.Items.Select(i => i.OrderNumber));
        report.Rows.Select(r => r.CustomerName).Should().Equal("Ayşe 0", "Ayşe 1", "Ayşe 2");
        report.Title.Should().Contain("Ayşe").And.Contain("Şirket").And.Contain("10.09.2026");
    }

    [Fact]
    public async Task OrderListExport_ShouldNotBeLimitedByPageSize()
    {
        await using var store = TestDb.CreateStore();
        var orders = new OrderService(store.Orders, new TestCurrentUser(), new OrderDraftValidator());
        for (var i = 0; i < 25; i++)
            await orders.CreateAsync(Draft($"Kişi {i}", new DateOnly(2026, 9, 10)));

        var report = await new ReportService(store.Reporting).GetForOrderListAsync(new OrderListFilter { PageSize = 20 });

        report.OrderCount.Should().Be(25);
        report.Title.Should().Contain("Tüm siparişler");
    }

    [Fact]
    public async Task OrderListExport_ShouldApplyScope()
    {
        await using var store = TestDb.CreateStore();
        var orders = new OrderService(store.Orders, new TestCurrentUser(), new OrderDraftValidator());
        await orders.CreateAsync(Draft("Açık", new DateOnly(2026, 9, 10)));
        var cancelledId = await orders.CreateAsync(Draft("İptal", new DateOnly(2026, 9, 10)));
        var cancelled = await orders.GetAsync(cancelledId);
        await orders.ChangeStatusAsync(cancelledId, OrderStatus.Iptal, cancelled!.RowVersion);

        var report = await new ReportService(store.Reporting).GetForOrderListAsync(
            new OrderListFilter { Scope = OrderListScope.Open, Status = OrderStatus.Iptal });

        report.Rows.Should().ContainSingle(r => r.CustomerName == "Açık");
        report.Title.Should().Contain(OrderScopes.Display(OrderListScope.Open));
    }

    private const string LongNote =
        "Müşteri kasaların arka kapağının mıknatıslı olmasını istedi. Teslimattan önce telefonla aranacak. " +
        "Fabrikadan çıkışta koruyucu streç ile sarılmalı, köşe takozları eksiksiz olmalıdır.";

    [Fact]
    public async Task PeriodReport_ShouldNotIncludeNotes()
    {
        await using var store = TestDb.CreateStore();
        var orders = new OrderService(store.Orders, new TestCurrentUser(), new OrderDraftValidator());
        var draft = Draft("Notlu", new DateOnly(2026, 9, 10));
        draft.Notes = LongNote;
        await orders.CreateAsync(draft);

        var report = await new ReportService(store.Reporting).GetAsync(new ReportRequest
        {
            Period = ReportPeriod.Daily,
            Anchor = new DateOnly(2026, 9, 10)
        });

        report.IncludeNotes.Should().BeFalse();
        report.Rows.Single().Notes.Should().BeNull();
        var text = ExcelTexts(new ExcelReportExporter().Export(report));
        text.Should().NotContain("Notlar");
        text.Should().NotContain(LongNote);
        text.Should().Contain("Ölçüler");
    }

    [Fact]
    public async Task OrderListExport_ShouldIncludeNotesInExcel()
    {
        await using var store = TestDb.CreateStore();
        var orders = new OrderService(store.Orders, new TestCurrentUser(), new OrderDraftValidator());
        var draft = Draft("Notlu", new DateOnly(2026, 9, 10));
        draft.Notes = LongNote;
        await orders.CreateAsync(draft);

        var report = await new ReportService(store.Reporting).GetForOrderListAsync(new OrderListFilter());

        report.IncludeNotes.Should().BeTrue();
        report.Rows.Single().Notes.Should().Be(LongNote);

        using var book = new XLWorkbook(new MemoryStream(new ExcelReportExporter().Export(report)));
        var sheet = book.Worksheet(1);
        var header = sheet.Row(5).CellsUsed().Single(c => c.GetString() == "Notlar");
        var noteCell = sheet.Cell(6, header.Address.ColumnNumber);
        noteCell.GetString().Should().Be(LongNote);
        noteCell.Style.Alignment.WrapText.Should().BeTrue();
        sheet.Column(header.Address.ColumnNumber).Width.Should().BeInRange(40, 80);
    }

    [Fact]
    public void Pdf_WithNotes_ShouldAddSpaceForWrappedNotes()
    {
        static ReportResult Build(bool includeNotes) => new()
        {
            Title = "Sipariş Listesi",
            IncludeNotes = includeNotes,
            Rows = Enumerable.Range(1, 12).Select(i => new ReportRow
            {
                OrderNumber = $"LK-20260910-{i:0000}",
                CustomerName = "Ayşe",
                OrderDate = new DateOnly(2026, 9, 10),
                DeliveryDate = new DateOnly(2026, 9, 12),
                DeliveryPlace = "Şirket",
                Status = "Yeni",
                ItemCount = 1,
                TotalQuantity = 1,
                ItemSummary = "CNC LED Kasa 50x50x5 cm Tek yön x1",
                Notes = string.Join(" ", Enumerable.Repeat(LongNote, 3))
            }).ToList()
        };

        var withNotes = new PdfReportExporter().Export(Build(true));
        var withoutNotes = new PdfReportExporter().Export(Build(false));

        PageCount(withNotes).Should().BeGreaterThan(PageCount(withoutNotes));
    }

    private static List<string> ExcelTexts(byte[] excel)
    {
        using var book = new XLWorkbook(new MemoryStream(excel));
        return book.Worksheet(1).RangeUsed()!.Cells().Select(c => c.GetString()).ToList();
    }

    private static int PageCount(byte[] pdf)
    {
        using var doc = PdfReader.Open(new MemoryStream(pdf), PdfDocumentOpenMode.Import);
        return doc.PageCount;
    }

    private static OrderDraft Draft(string name, DateOnly date) => new()
    {
        CustomerName = name,
        OrderDate = date,
        DeliveryDate = date.AddDays(2),
        DeliveryPlace = DeliveryPlace.Sirket,
        Items = [new OrderItemInput { ProductId = 1, WidthCm = 50, HeightCm = 50, Quantity = 1 }]
    };
}
