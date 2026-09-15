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
    public void ShouldRenderLabeledMeasureAndQuantity()
    {
        var item = new OrderItemDto
        {
            WidthCm = 123,
            HeightCm = 12,
            Quantity = 2,
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
        cut.Markup.Should().Contain("Köşe kesim");
        cut.Markup.Should().NotContain("Tutar");
        cut.Markup.Should().NotContain("Fiyat");
    }
}
