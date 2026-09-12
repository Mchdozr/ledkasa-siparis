using Bunit;
using Bunit.TestDoubles;
using FluentAssertions;
using LedKasa.Siparis.Components.Shared;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;

namespace LedKasa.Siparis.Tests.Components;

public class PageHeaderTests : TestContext
{
    public PageHeaderTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
    }

    [Fact]
    public void Nested_ShouldShowParentCrumbAndBack()
    {
        var cut = RenderComponent<PageHeader>(p => p
            .Add(x => x.Title, "Yeni sipariş")
            .Add(x => x.Parent, "Siparişler")
            .Add(x => x.BackHref, "/orders"));

        cut.Find("a").GetAttribute("href").Should().Be("/orders");
        cut.Markup.Should().Contain("Siparişler");
        cut.Markup.Should().Contain("Yeni sipariş");
        cut.Find("button.lk-back").Click();
        Services.GetRequiredService<FakeNavigationManager>().Uri.Should().EndWith("/orders");
    }

    [Fact]
    public void Root_ShouldShowTitleWithoutBack()
    {
        var cut = RenderComponent<PageHeader>(p => p.Add(x => x.Title, "Siparişler"));
        cut.Find("h1").TextContent.Should().Be("Siparişler");
        cut.FindAll("button.lk-back").Should().BeEmpty();
    }
}
