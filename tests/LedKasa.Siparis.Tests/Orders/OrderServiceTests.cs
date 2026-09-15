using FluentAssertions;
using FluentValidation;
using LedKasa.Siparis.Common;
using LedKasa.Siparis.Features.Orders;
using LedKasa.Siparis.Features.Orders.Domain;
using LedKasa.Siparis.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace LedKasa.Siparis.Tests.Orders;

public class OrderServiceTests
{
    [Fact]
    public async Task Create_ShouldAssignDailyNumber_AndList()
    {
        await using var db = TestDb.Create();
        var service = CreateService(db);
        var draft = Draft("Ayşe Kaya");

        var id = await service.CreateAsync(draft);
        var detail = await service.GetAsync(id);
        var list = await service.ListAsync(new OrderListFilter { Search = "Ayşe" });

        id.Should().BeGreaterThan(0);
        detail!.OrderNumber.Should().StartWith("LK-");
        detail.Items.Should().HaveCount(1);
        detail.GrandTotal.Should().Be(400);
        detail.Items[0].UnitPrice.Should().Be(100);
        detail.Items[0].LineTotal.Should().Be(400);
        detail.DeliveryPlace.Should().Be(DeliveryPlace.Fabrika);
        list.Items[0].GrandTotal.Should().Be(400);
        list.Items[0].DeliveryPlace.Should().Be(DeliveryPlace.Fabrika);
        list.TotalCount.Should().Be(1);
        db.AuditLogs.Should().Contain(a => a.Action == "OrderCreated");
    }

    [Fact]
    public async Task Create_ShouldNotifyTelegram()
    {
        await using var db = TestDb.Create();
        var telegram = new RecordingTelegramNotifier();
        var service = new OrderService(
            db,
            new TestCurrentUser { DisplayName = "Admin" },
            new OrderDraftValidator(),
            telegram);

        var id = await service.CreateAsync(Draft("Ayşe Kaya"));

        telegram.CallCount.Should().Be(1);
        telegram.Last.Should().NotBeNull();
        telegram.Last!.Id.Should().Be(id);
        telegram.Last.CustomerName.Should().Be("Ayşe Kaya");
        telegram.Last.CreatedBy.Should().Be("Admin");
        telegram.Last.Lines.Should().ContainSingle();
        telegram.Last.Lines[0].ProductName.Should().Be("Rental LED Kabinet");
        telegram.Last.Lines[0].Extras.Should().Contain("Köşe kesim");
    }

    [Fact]
    public async Task Create_ShouldReject_InvalidDates()
    {
        await using var db = TestDb.Create();
        var service = CreateService(db);
        var draft = Draft("Ali");
        draft.DeliveryDate = draft.OrderDate.AddDays(-1);

        var act = async () => await service.CreateAsync(draft);
        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task ChangeStatus_ShouldMoveToOnaylandi()
    {
        await using var db = TestDb.Create();
        var service = CreateService(db);
        var id = await service.CreateAsync(Draft("Deniz"));
        var created = await service.GetAsync(id);

        await service.ChangeStatusAsync(id, OrderStatus.Onaylandi, created!.RowVersion);
        var updated = await service.GetAsync(id);

        updated!.Status.Should().Be(OrderStatus.Onaylandi);
    }

    [Fact]
    public async Task Staff_ShouldNotSeePrices_OnGetAndList()
    {
        await using var db = TestDb.Create();
        var admin = CreateService(db);
        var id = await admin.CreateAsync(Draft("Gizli Fiyat"));
        var staff = CreateService(db, canViewPrices: false);

        var detail = await staff.GetAsync(id);
        var list = await staff.ListAsync(new OrderListFilter());

        detail!.GrandTotal.Should().Be(0);
        detail.Items[0].UnitPrice.Should().Be(0);
        detail.Items[0].LineTotal.Should().Be(0);
        list.Items[0].GrandTotal.Should().Be(0);
    }

    [Fact]
    public async Task StaffCreate_ShouldBeRejected()
    {
        await using var db = TestDb.Create();
        var staff = CreateService(db, canViewPrices: false, canCreateOrders: false);

        var act = async () => await staff.CreateAsync(Draft("Personel"));
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*oluşturma yetkiniz yok*");
        db.Orders.Should().BeEmpty();
    }

    [Fact]
    public async Task StaffUpdate_ShouldKeepExistingPrices()
    {
        await using var db = TestDb.Create();
        var admin = CreateService(db);
        var createdId = await admin.CreateAsync(Draft("Korunan"));
        var created = await admin.GetAsync(createdId);

        var staff = CreateService(db, canViewPrices: false, canCreateOrders: false);
        var draft = Draft("Korunan");
        draft.Items[0].Id = created!.Items[0].Id;
        draft.Items[0].Quantity = 2;
        draft.Items[0].UnitPrice = 1;

        await staff.UpdateAsync(createdId, draft, created.RowVersion);

        var stored = await db.Orders.AsNoTracking().Include(o => o.Items).SingleAsync(o => o.Id == createdId);
        stored.Items.Single().UnitPrice.Should().Be(100);
        stored.Items.Single().Quantity.Should().Be(2);
        stored.GrandTotal.Should().Be(200);
    }

    [Fact]
    public async Task List_ShouldApplySelectedSort()
    {
        await using var db = TestDb.Create();
        var service = CreateService(db);
        await service.CreateAsync(Draft("Orta", new DateOnly(2026, 9, 10), new DateOnly(2026, 9, 20)));
        await service.CreateAsync(Draft("Yeni", new DateOnly(2026, 9, 12), new DateOnly(2026, 9, 15)));
        await service.CreateAsync(Draft("Eski", new DateOnly(2026, 9, 8), new DateOnly(2026, 9, 30)));

        (await Names(service, OrderSort.NewestFirst)).Should().Equal("Yeni", "Orta", "Eski");
        (await Names(service, OrderSort.OldestFirst)).Should().Equal("Eski", "Orta", "Yeni");
        (await Names(service, OrderSort.NearestDelivery)).Should().Equal("Yeni", "Orta", "Eski");
        (await Names(service, OrderSort.FarthestDelivery)).Should().Equal("Eski", "Orta", "Yeni");
    }

    [Fact]
    public async Task List_ShouldApplyDashboardScopes()
    {
        await using var db = TestDb.Create();
        var service = CreateService(db);
        var today = TurkeyTime.Today;
        var week = LedKasa.Siparis.Features.Reports.ReportPeriodCalculator.GetRange(
            LedKasa.Siparis.Features.Reports.ReportPeriod.Weekly, today);

        await service.CreateAsync(Draft("Bugun", today, today.AddDays(10)));
        await service.CreateAsync(Draft("DunAcik", today.AddDays(-1), week.To));
        var teslimId = await service.CreateAsync(Draft("Teslim", week.From, week.From));
        await MoveTo(service, teslimId, OrderStatus.TeslimEdildi);
        var gecikenId = await service.CreateAsync(Draft("Geciken", week.From.AddDays(-10), week.From.AddDays(-1)));
        await MoveTo(service, gecikenId, OrderStatus.Onaylandi);
        var iptalId = await service.CreateAsync(Draft("IptalHafta", week.From, week.From));
        await MoveTo(service, iptalId, OrderStatus.Iptal);

        (await Names(service, scope: OrderListScope.Today)).Should().Equal("Bugun");
        (await Names(service, scope: OrderListScope.Open)).Should().BeEquivalentTo("Bugun", "DunAcik", "Geciken");
        (await Names(service, scope: OrderListScope.WeekDelivery)).Should().BeEquivalentTo("DunAcik", "Teslim");
        (await Names(service, scope: OrderListScope.Overdue)).Should().Equal("Geciken");
    }

    private static async Task<string[]> Names(OrderService service, OrderSort sort = OrderSort.NewestFirst, OrderListScope scope = OrderListScope.None)
    {
        var list = await service.ListAsync(new OrderListFilter { Sort = sort, Scope = scope });
        return list.Items.Select(x => x.CustomerName).ToArray();
    }

    private static async Task MoveTo(OrderService service, int id, OrderStatus target)
    {
        foreach (var next in PathTo(target))
        {
            var current = await service.GetAsync(id);
            await service.ChangeStatusAsync(id, next, current!.RowVersion);
        }
    }

    private static IEnumerable<OrderStatus> PathTo(OrderStatus target) => target switch
    {
        OrderStatus.Yeni => [],
        OrderStatus.Onaylandi => [OrderStatus.Onaylandi],
        OrderStatus.Uretimde => [OrderStatus.Onaylandi, OrderStatus.Uretimde],
        OrderStatus.Hazir => [OrderStatus.Onaylandi, OrderStatus.Uretimde, OrderStatus.Hazir],
        OrderStatus.TeslimEdildi =>
        [
            OrderStatus.Onaylandi, OrderStatus.Uretimde, OrderStatus.Hazir, OrderStatus.TeslimEdildi
        ],
        OrderStatus.Iptal => [OrderStatus.Iptal],
        _ => throw new ArgumentOutOfRangeException(nameof(target), target, null)
    };

    private static OrderService CreateService(
        LedKasa.Siparis.Data.ApplicationDbContext db,
        bool canViewPrices = true,
        bool canCreateOrders = true)
        => new(db, new TestCurrentUser { CanViewPrices = canViewPrices, CanCreateOrders = canCreateOrders }, new OrderDraftValidator(), new NullTelegramNotifier());

    private static OrderDraft Draft(string name, DateOnly? orderDate = null, DateOnly? deliveryDate = null) => new()
    {
        CustomerName = name,
        OrderDate = orderDate ?? new DateOnly(2026, 9, 10),
        DeliveryDate = deliveryDate ?? new DateOnly(2026, 9, 12),
        DeliveryPlace = DeliveryPlace.Fabrika,
        Items =
        [
            new OrderItemInput
            {
                ProductId = 3,
                WidthCm = 96,
                HeightCm = 96,
                Quantity = 4,
                UnitPrice = 100,
                ExtraFeatureIds = [1],
                Note = "rental"
            }
        ]
    };
}
