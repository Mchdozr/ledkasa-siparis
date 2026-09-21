using Bunit;
using Bunit.TestDoubles;
using FluentAssertions;
using LedKasa.Siparis.Components.Pages.Orders;
using LedKasa.Siparis.Features.Catalog;
using LedKasa.Siparis.Features.Orders;
using LedKasa.Siparis.Features.Orders.Domain;
using LedKasa.Siparis.Identity;
using LedKasa.Siparis.Security;
using LedKasa.Siparis.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;

namespace LedKasa.Siparis.Tests.Components;

public class OrderEditErrorTests : TestContext
{
    public OrderEditErrorTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
        Services.AddLogging();
        var auth = this.AddTestAuthorization();
        auth.SetAuthorized("test");
        auth.SetPolicies(Policies.OrdersEdit, Policies.OrdersCreate);
        auth.SetRoles(AppRoles.SiparisPersoneli);
        Services.AddSingleton<ICurrentUser>(new TestCurrentUser { CanCreateOrders = true });
        Services.AddSingleton<IOrderService>(new DummyOrders());
        Services.AddSingleton<IProductService>(new ThrowingProducts());
    }

    [Fact]
    public void ShouldShowRecoverableMessage_WhenProductsFail()
    {
        var cut = Render(builder =>
        {
            builder.OpenComponent<MudPopoverProvider>(0);
            builder.CloseComponent();
            builder.OpenComponent<MudSnackbarProvider>(1);
            builder.CloseComponent();
            builder.OpenComponent<OrderEdit>(2);
            builder.CloseComponent();
        });

        cut.Markup.Should().Contain("Sipariş formu yüklenemedi");
        cut.Markup.Should().NotContain("An error occurred");
    }

    private sealed class ThrowingProducts : IProductService
    {
        public Task<IReadOnlyList<ProductDto>> ListAsync(bool activeOnly, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("A second operation was started on this context instance");

        public Task<int> CreateAsync(string name, CancellationToken cancellationToken = default) => Task.FromResult(0);

        public Task UpdateAsync(int id, string name, bool isActive, int sortOrder, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class DummyOrders : IOrderService
    {
        public Task<PagedResult<OrderListItemDto>> ListAsync(OrderListFilter filter, CancellationToken cancellationToken = default)
            => Task.FromResult(new PagedResult<OrderListItemDto> { Items = [], TotalCount = 0, Page = 1, PageSize = 20 });

        public Task<OrderDetailDto?> GetAsync(int id, CancellationToken cancellationToken = default)
            => Task.FromResult<OrderDetailDto?>(null);

        public Task<int> CreateAsync(OrderDraft draft, CancellationToken cancellationToken = default) => Task.FromResult(0);

        public Task UpdateAsync(int id, OrderDraft draft, DateTime rowVersion, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task ChangeStatusAsync(int id, OrderStatus next, DateTime rowVersion, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<PersonSuggestions> ListPersonSuggestionsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new PersonSuggestions([], []));
    }
}
