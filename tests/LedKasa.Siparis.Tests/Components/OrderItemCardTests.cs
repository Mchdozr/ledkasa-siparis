using Bunit;
using FluentAssertions;
using LedKasa.Siparis.Components.Shared;
using LedKasa.Siparis.Features.Orders;
using LedKasa.Siparis.Features.Orders.Domain;
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
    public void ShouldRenderLabeledMeasureQuantityAndSide()
    {
        var item = new OrderItemDto
        {
            WidthCm = 123,
            HeightCm = 12,
            DepthCm = 5,
            Quantity = 2,
            Side = PanelSide.TekYon,
            ProductName = "CNC LED Kasa",
            Note = "not"
        };

        var cut = RenderComponent<OrderItemCard>(p => p
            .Add(x => x.Item, item));

        cut.Markup.Should().Contain("CNC LED Kasa");
        cut.Markup.Should().Contain("Ölçü");
        cut.Markup.Should().Contain("123 × 12 × 5 cm");
        cut.Markup.Should().Contain("Yön");
        cut.Markup.Should().Contain("Tek yön");
        cut.Markup.Should().Contain("Adet");
        cut.Markup.Should().Contain(">2<");
        cut.Markup.Should().Contain("not");
        cut.Markup.Should().NotContain("Tutar");
        cut.Markup.Should().NotContain("Fiyat");
        cut.Markup.Should().NotContain("Ekstra");
        cut.Markup.Should().NotContain(",");
    }
}
