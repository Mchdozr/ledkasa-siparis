using Bunit;
using FluentAssertions;
using LedKasa.Siparis.Components.Pages;
using Microsoft.Extensions.DependencyInjection;

namespace LedKasa.Siparis.Tests.Components;

public class ErrorPageTests : TestContext
{
    public ErrorPageTests()
    {
        Services.AddLogging();
    }
    [Fact]
    public void ShouldShowTurkishMessage_WithoutDevelopmentLeak()
    {
        var cut = RenderComponent<Error>();

        cut.Markup.Should().Contain("Bir hata oluştu");
        cut.Markup.Should().Contain("Siparişler");
        cut.Markup.Should().NotContain("Development Mode");
        cut.Markup.Should().NotContain("An error occurred");
    }
}
