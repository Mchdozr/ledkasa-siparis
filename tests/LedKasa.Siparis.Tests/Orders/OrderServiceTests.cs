using FluentAssertions;
using FluentValidation;
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
        detail.GrandTotal.Should().Be(400);
        detail.Items[0].UnitPrice.Should().Be(100);
        detail.Items[0].LineTotal.Should().Be(400);
        detail.Currency.Should().Be(Currency.Try);
        list.Items[0].GrandTotal.Should().Be(400);
        list.Items[0].Currency.Should().Be(Currency.Try);
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

    private static OrderService CreateService(LedKasa.Siparis.Data.ApplicationDbContext db)
        => new(db, new TestCurrentUser(), new OrderDraftValidator());

    private static OrderDraft Draft(string name) => new()
    {
        CustomerName = name,
        OrderDate = new DateOnly(2026, 9, 10),
        DeliveryDate = new DateOnly(2026, 9, 12),
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
