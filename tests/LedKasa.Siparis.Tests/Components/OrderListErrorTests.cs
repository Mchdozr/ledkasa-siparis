using Bunit;
using Bunit.TestDoubles;
using FluentAssertions;
using LedKasa.Siparis.Components.Pages.Orders;
using LedKasa.Siparis.Features.Orders;
using LedKasa.Siparis.Features.Orders.Domain;
using LedKasa.Siparis.Identity;
using LedKasa.Siparis.Security;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;

namespace LedKasa.Siparis.Tests.Components;

public class OrderListErrorTests : TestContext
{
    public OrderListErrorTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
        Services.AddLogging();
        var auth = this.AddTestAuthorization();
        auth.SetAuthorized("test");
        auth.SetPolicies(Policies.OrdersView, Policies.OrdersCreate);
        auth.SetRoles(AppRoles.SiparisPersoneli);
        Services.AddSingleton<IOrderService>(new ThrowingOrders());
    }

    [Fact]
    public void ShouldShowRecoverableMessage_WhenListFails()
    {
        var cut = Render(builder =>
        {
            builder.OpenComponent<MudPopoverProvider>(0);
            builder.CloseComponent();
            builder.OpenComponent<OrderList>(1);
            builder.CloseComponent();
        });

        cut.Markup.Should().Contain("Siparişler yüklenemedi");
        cut.Markup.Should().NotContain("An error occurred");
        cut.FindAll("button").Should().Contain(b => b.TextContent.Contains("Yeniden dene"));
    }

    private sealed class ThrowingOrders : IOrderService
    {
        public Task<PagedResult<OrderListItemDto>> ListAsync(OrderListFilter filter, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("A second operation was started on this context instance");

        public Task<OrderDetailDto?> GetAsync(int id, CancellationToken cancellationToken = default)
            => Task.FromResult<OrderDetailDto?>(null);

        public Task<int> CreateAsync(OrderDraft draft, CancellationToken cancellationToken = default)
            => Task.FromResult(0);

        public Task UpdateAsync(int id, OrderDraft draft, DateTime rowVersion, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task ChangeStatusAsync(int id, OrderStatus next, DateTime rowVersion, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<PersonSuggestions> ListPersonSuggestionsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new PersonSuggestions([], []));
    }
}
