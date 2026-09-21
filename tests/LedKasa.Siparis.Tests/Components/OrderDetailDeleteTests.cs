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

public class OrderDetailDeleteTests : TestContext
{
    public OrderDetailDeleteTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
        Services.AddLogging();
        Services.AddSingleton<IOrderService>(new FakeOrders());
    }

    [Fact]
    public void ShouldShowDelete_WhenPolicyGranted()
    {
        Authorize(Policies.OrdersView, Policies.OrdersDelete);
        RenderDetail().Markup.Should().Contain(">Sil<");
    }

    [Fact]
    public void ShouldHideDelete_WhenPolicyMissing()
    {
        Authorize(Policies.OrdersView, Policies.OrdersEdit);
        RenderDetail().Markup.Should().NotContain(">Sil<");
    }

    private void Authorize(params string[] policies)
    {
        var auth = this.AddTestAuthorization();
        auth.SetAuthorized("admin");
        auth.SetPolicies(policies);
        auth.SetRoles(AppRoles.Yonetici);
    }

    private IRenderedFragment RenderDetail() =>
        Render(builder =>
        {
            builder.OpenComponent<MudPopoverProvider>(0);
            builder.CloseComponent();
            builder.OpenComponent<MudDialogProvider>(1);
            builder.CloseComponent();
            builder.OpenComponent<OrderDetail>(2);
            builder.AddAttribute(3, nameof(OrderDetail.Id), 9);
            builder.CloseComponent();
        });

    private sealed class FakeOrders : IOrderService
    {
        public Task<PagedResult<OrderListItemDto>> ListAsync(OrderListFilter filter, CancellationToken cancellationToken = default)
            => Task.FromResult(new PagedResult<OrderListItemDto> { Items = [], TotalCount = 0, Page = 1, PageSize = 20 });

        public Task<OrderDetailDto?> GetAsync(int id, CancellationToken cancellationToken = default)
            => Task.FromResult<OrderDetailDto?>(new OrderDetailDto
            {
                Id = 9,
                OrderNumber = "LK-20260921-0001",
                CustomerName = "Ayşe",
                OrderDate = new DateOnly(2026, 9, 10),
                DeliveryDate = new DateOnly(2026, 9, 12),
                DeliveryPlace = DeliveryPlace.Fabrika,
                Status = OrderStatus.Yeni,
                CanEdit = true,
                RowVersion = DateTime.UtcNow
            });

        public Task<int> CreateAsync(OrderDraft draft, CancellationToken cancellationToken = default) => Task.FromResult(0);

        public Task UpdateAsync(int id, OrderDraft draft, DateTime rowVersion, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task ChangeStatusAsync(int id, OrderStatus next, DateTime rowVersion, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task DeleteAsync(int id, DateTime rowVersion, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<PersonSuggestions> ListPersonSuggestionsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new PersonSuggestions([], []));
    }
}
