using Bunit;
using Bunit.TestDoubles;
using FluentAssertions;
using LedKasa.Siparis.Components.Layout;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;

namespace LedKasa.Siparis.Tests.Components;

public class MainLayoutBrandTests : TestContext
{
    public MainLayoutBrandTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
        this.AddTestAuthorization();
        Services.AddSingleton<IBrowserViewportService>(new FakeViewport());
    }

    [Fact]
    public void Brand_ShouldGoHome()
    {
        var cut = RenderComponent<MainLayout>(p => p.Add(x => x.Body, builder => builder.AddContent(0, "içerik")));
        var brand = cut.Find("a.lk-brand");
        brand.GetAttribute("href").Should().Be("/");
        brand.TextContent.Should().Contain("LEDKASA");
    }

    private sealed class FakeViewport : IBrowserViewportService
    {
        public ResizeOptions ResizeOptions { get; } = new();

        public Task<BrowserWindowSize> GetCurrentBrowserWindowSizeAsync()
            => Task.FromResult(new BrowserWindowSize { Width = 1440, Height = 900 });

        public Task<Breakpoint> GetCurrentBreakpointAsync() => Task.FromResult(Breakpoint.Lg);

        public Task<bool> IsBreakpointWithinReferenceSizeAsync(Breakpoint breakpoint, Breakpoint reference)
            => Task.FromResult(true);

        public Task<bool> IsBreakpointWithinWindowSizeAsync(Breakpoint breakpoint)
            => Task.FromResult(true);

        public Task SubscribeAsync(IBrowserViewportObserver observer, bool fireImmediately = true)
            => Task.CompletedTask;

        public Task SubscribeAsync(Guid id, Action<BrowserViewportEventArgs> callback, ResizeOptions? options = null, bool fireImmediately = true)
            => Task.CompletedTask;

        public Task SubscribeAsync(Guid id, Func<BrowserViewportEventArgs, Task> callback, ResizeOptions? options = null, bool fireImmediately = true)
            => Task.CompletedTask;

        public Task UnsubscribeAsync(IBrowserViewportObserver observer) => Task.CompletedTask;

        public Task UnsubscribeAsync(Guid id) => Task.CompletedTask;

        public Task<bool> IsMediaQueryMatchAsync(string mediaQuery) => Task.FromResult(false);

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
