using FluentAssertions;
using FluentValidation;
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
        detail.Currency.Should().Be(Currency.Usd);
        detail.DeliveryAddress.Should().Be("Fabrika deposu, Organize Sanayi");
        list.Items[0].GrandTotal.Should().Be(400);
        list.Items[0].Currency.Should().Be(Currency.Usd);
        list.TotalCount.Should().Be(1);
        db.AuditLogs.Should().Contain(a => a.Action == "OrderCreated");
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
    public async Task Create_ShouldPersistUsdAndShowOnList()
    {
        await using var db = TestDb.Create();
        var service = CreateService(db);
        var draft = Draft("Dolar");
        draft.Currency = Currency.Usd;

        var id = await service.CreateAsync(draft);
        var detail = await service.GetAsync(id);
        var list = await service.ListAsync(new OrderListFilter());

        detail!.Currency.Should().Be(Currency.Usd);
        list.Items[0].Currency.Should().Be(Currency.Usd);
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
        draft.Currency = Currency.Eur;

        await staff.UpdateAsync(createdId, draft, created.RowVersion);

        var stored = await db.Orders.AsNoTracking().Include(o => o.Items).SingleAsync(o => o.Id == createdId);
        stored.Currency.Should().Be(Currency.Usd);
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

    private static async Task<string[]> Names(OrderService service, OrderSort sort)
    {
        var list = await service.ListAsync(new OrderListFilter { Sort = sort });
        return list.Items.Select(x => x.CustomerName).ToArray();
    }

    private static OrderService CreateService(
        LedKasa.Siparis.Data.ApplicationDbContext db,
        bool canViewPrices = true,
        bool canCreateOrders = true)
        => new(db, new TestCurrentUser { CanViewPrices = canViewPrices, CanCreateOrders = canCreateOrders }, new OrderDraftValidator());

    private static OrderDraft Draft(string name, DateOnly? orderDate = null, DateOnly? deliveryDate = null) => new()
    {
        CustomerName = name,
        OrderDate = orderDate ?? new DateOnly(2026, 9, 10),
        DeliveryDate = deliveryDate ?? new DateOnly(2026, 9, 12),
        DeliveryPlace = DeliveryPlace.Fabrika,
        DeliveryAddress = "Fabrika deposu, Organize Sanayi",
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
