using FluentAssertions;
using FluentValidation;
using LedKasa.Siparis.Common;
using LedKasa.Siparis.Features.Orders;
using LedKasa.Siparis.Features.Orders.Domain;
using LedKasa.Siparis.Tests.Infrastructure;

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
        detail.Items[0].DepthCm.Should().Be(5);
        detail.Items[0].Side.Should().Be(PanelSide.TekYon);
        detail.DeliveryPlace.Should().Be(DeliveryPlace.Fabrika);
        list.Items[0].TotalQuantity.Should().Be(4);
        list.TotalCount.Should().Be(1);
        db.AuditLogs.Should().Contain(a => a.Action == "OrderCreated");
    }

    [Fact]
    public async Task Create_ShouldPersistDepthAndCiftYon()
    {
        await using var db = TestDb.Create();
        var service = CreateService(db);
        var draft = Draft("Çift yön");
        draft.Items[0].DepthCm = 8;
        draft.Items[0].Side = PanelSide.CiftYon;

        var id = await service.CreateAsync(draft);
        var detail = await service.GetAsync(id);

        detail!.Items[0].DepthCm.Should().Be(8);
        detail.Items[0].Side.Should().Be(PanelSide.CiftYon);
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
    public async Task Create_ShouldPersistDeliveryPlaceAndShowOnList()
    {
        await using var db = TestDb.Create();
        var service = CreateService(db);
        var draft = Draft("FabrikaTeslim");
        draft.DeliveryPlace = DeliveryPlace.Fabrika;

        var id = await service.CreateAsync(draft);
        var detail = await service.GetAsync(id);
        var list = await service.ListAsync(new OrderListFilter());

        detail!.DeliveryPlace.Should().Be(DeliveryPlace.Fabrika);
        list.Items[0].DeliveryPlace.Should().Be(DeliveryPlace.Fabrika);
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
    public async Task StaffCreate_ShouldBeRejected()
    {
        await using var db = TestDb.Create();
        var staff = CreateService(db, canCreateOrders: false);

        var act = async () => await staff.CreateAsync(Draft("Personel"));
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*oluşturma yetkiniz yok*");
        db.Orders.Should().BeEmpty();
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
        bool canCreateOrders = true)
        => new(db, new TestCurrentUser { CanCreateOrders = canCreateOrders }, new OrderDraftValidator());

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
                Note = "rental"
            }
        ]
    };
}
