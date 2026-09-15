using Bunit;
using FluentAssertions;
using LedKasa.Siparis.Components.Shared;
using LedKasa.Siparis.Features.Orders;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;

namespace LedKasa.Siparis.Tests.Components;

public class OrderItemCardTests : TestContext
{
    public OrderItemCardTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
    }

    [Fact]
    public void ShouldRenderLabeledMeasureQuantityAndMoney()
    {
        var item = new OrderItemDto
        {
            WidthCm = 123,
            HeightCm = 12,
            Quantity = 2,
            UnitPrice = 15,
            LineTotal = 30,
            ProductName = "CNC LED Kasa",
            ExtraFeatureNames = ["Köşe kesim"],
            Note = "not"
        };

        var cut = RenderComponent<OrderItemCard>(p => p
            .Add(x => x.Item, item));

        cut.Markup.Should().Contain("CNC LED Kasa");
        cut.Markup.Should().Contain("Ölçü");
        cut.Markup.Should().Contain("123 × 12 cm");
        cut.Markup.Should().Contain("Adet");
        cut.Markup.Should().Contain(">2<");
        cut.Markup.Should().Contain("15,00 × 2 = 30,00");
        cut.Markup.Should().Contain("Köşe kesim");
    }

    [Fact]
    public void ShouldHideMoney_WhenShowPricesFalse()
    {
        var item = new OrderItemDto
        {
            WidthCm = 80,
            HeightCm = 120,
            Quantity = 2,
            UnitPrice = 15,
            LineTotal = 30,
            ProductName = "CNC LED Kasa"
        };

        var cut = RenderComponent<OrderItemCard>(p => p
            .Add(x => x.Item, item)
            .Add(x => x.ShowPrices, false));

        cut.Markup.Should().Contain("CNC LED Kasa");
        cut.Markup.Should().Contain("80 × 120 cm");
        cut.Markup.Should().NotContain("Fiyat");
        cut.Markup.Should().NotContain("15,00");
    }
}
