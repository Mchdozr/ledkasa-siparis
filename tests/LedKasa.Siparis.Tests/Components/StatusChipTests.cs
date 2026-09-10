using Bunit;
using FluentAssertions;
using LedKasa.Siparis.Components.Shared;
using LedKasa.Siparis.Features.Orders.Domain;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;

namespace LedKasa.Siparis.Tests.Components;

public class StatusChipTests : TestContext
{
    public StatusChipTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
    }

    [Fact]
    public void ShouldRenderTurkishStatus()
    {
        var cut = RenderComponent<StatusChip>(p => p.Add(x => x.Status, OrderStatus.Uretimde));
        cut.Markup.Should().Contain("Üretimde");
    }
}
