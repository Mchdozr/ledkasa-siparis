using FluentAssertions;
using LedKasa.Siparis.Features.Catalog;
using LedKasa.Siparis.Features.Orders;
using LedKasa.Siparis.Features.Orders.Domain;
using LedKasa.Siparis.Tests.Infrastructure;

namespace LedKasa.Siparis.Tests.Orders;

public class CircuitConcurrencyTests
{
    [Fact]
    public async Task PersonSuggestionsAndProductList_ShouldRunTogether()
    {
        await using var store = TestDb.CreateStore();
        var orders = new OrderService(store.Orders, new TestCurrentUser(), new OrderDraftValidator());
        var products = new ProductService(store.Catalog, new TestCurrentUser());
        await orders.CreateAsync(new OrderDraft
        {
            CustomerName = "Ayşe Kaya",
            OrderDate = new DateOnly(2026, 9, 10),
            DeliveryDate = new DateOnly(2026, 9, 12),
            DeliveryPlace = DeliveryPlace.Fabrika,
            Items = [new OrderItemInput { ProductId = 3, WidthCm = 96, HeightCm = 96, Quantity = 1 }]
        });

        var suggestionTask = orders.ListPersonSuggestionsAsync();
        var productTask = products.ListAsync(activeOnly: true);

        var act = async () => await Task.WhenAll(suggestionTask, productTask);
        await act.Should().NotThrowAsync();

        (await suggestionTask).PreviousCustomers.Should().Contain("Ayşe Kaya");
        (await productTask).Should().Contain(p => p.Name == "CNC LED Kasa");
    }
}
