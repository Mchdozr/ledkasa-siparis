using Bunit;
using Bunit.TestDoubles;
using FluentAssertions;
using LedKasa.Siparis.Components.Pages.Orders;
using LedKasa.Siparis.Components.Shared;
using LedKasa.Siparis.Features.Orders;
using LedKasa.Siparis.Features.Orders.Domain;
using LedKasa.Siparis.Identity;
using LedKasa.Siparis.Security;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;

namespace LedKasa.Siparis.Tests.Components;

public class OrderListStatusQueryTests : TestContext
{
    private readonly CapturingOrders _orders = new();

    public OrderListStatusQueryTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
        Services.AddLogging();
        var auth = this.AddTestAuthorization();
        auth.SetAuthorized("test");
        auth.SetPolicies(Policies.OrdersView, Policies.OrdersCreate);
        auth.SetRoles(AppRoles.SiparisPersoneli);
        Services.AddSingleton<IOrderService>(_orders);
    }

    [Fact]
    public void StatusQueryIptal_ShouldRenderWithoutServerError()
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo("/orders?status=6");
        var cut = RenderList();

        _orders.LastFilter!.Status.Should().Be(OrderStatus.Iptal);
        cut.Markup.Should().Contain("İptal");
        cut.Markup.Should().Contain("LK-IPTAL");
        cut.Markup.Should().NotContain("An error occurred");
        cut.Markup.Should().NotContain("Siparişler yüklenemedi");
    }

    [Fact]
    public void SearchBox_ShouldUseCustomerSuggestions()
    {
        var cut = RenderList();

        var field = cut.FindComponent<CustomerNameField>();
        field.Instance.Label.Should().Be("Sipariş no / kişi");
        field.Instance.Required.Should().BeFalse();
        _orders.SuggestionCalls.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task SelectingSuggestion_ShouldApplySearch()
    {
        var cut = RenderList();
        var field = cut.FindComponent<CustomerNameField>();

        await cut.InvokeAsync(() => field.Instance.ValueChanged.InvokeAsync("Ayşe Yılmaz"));

        _orders.LastFilter!.Search.Should().Be("Ayşe Yılmaz");
    }

    [Fact]
    public async Task EnterInSearch_ShouldApplyTypedText()
    {
        var cut = RenderList();
        var field = cut.FindComponent<CustomerNameField>();

        await cut.InvokeAsync(() => field.Instance.ValueChanged.InvokeAsync("LK-2026"));
        var calls = _orders.ListCalls;
        await cut.InvokeAsync(() => field.Instance.OnEnter.InvokeAsync());

        _orders.LastFilter!.Search.Should().Be("LK-2026");
        _orders.ListCalls.Should().Be(calls + 1);
    }

    [Fact]
    public async Task SearchSuggestions_ShouldKeepTypedTextFirst()
    {
        var cut = RenderList();
        var autocomplete = cut.FindComponent<MudAutocomplete<string>>();

        var partial = (await autocomplete.Instance.SearchFunc!("Ayş", CancellationToken.None)).ToList();
        var orderNo = (await autocomplete.Instance.SearchFunc!("LK-2026", CancellationToken.None)).ToList();

        partial.Should().Equal("Ayş", "Ayşe Yılmaz");
        orderNo.Should().Equal("LK-2026");
    }

    [Fact]
    public async Task EmptySearch_FirstSuggestion_ShouldShowAllOrders()
    {
        var cut = RenderList();
        await cut.InvokeAsync(() => cut.FindComponent<CustomerNameField>().Instance.ValueChanged.InvokeAsync("Ayşe Yılmaz"));
        var autocomplete = cut.FindComponent<MudAutocomplete<string>>();

        var empty = (await autocomplete.Instance.SearchFunc!("", CancellationToken.None)).ToList();
        empty.Should().Equal("", "Ayşe Yılmaz");

        await cut.InvokeAsync(() => cut.FindComponent<CustomerNameField>().Instance.ValueChanged.InvokeAsync(empty[0]));
        _orders.LastFilter!.Search.Should().BeNull();
    }

    private IRenderedFragment RenderList()
    {
        return Render(builder =>
        {
            builder.OpenComponent<MudPopoverProvider>(0);
            builder.CloseComponent();
            builder.OpenComponent<OrderList>(1);
            builder.CloseComponent();
        });
    }

    private sealed class CapturingOrders : IOrderService
    {
        public OrderListFilter? LastFilter { get; private set; }
        public int ListCalls { get; private set; }
        public int SuggestionCalls { get; private set; }

        public Task<PagedResult<OrderListItemDto>> ListAsync(OrderListFilter filter, CancellationToken cancellationToken = default)
        {
            LastFilter = filter;
            ListCalls++;
            return Task.FromResult(new PagedResult<OrderListItemDto>
            {
                Items =
                [
                    new OrderListItemDto
                    {
                        Id = 9,
                        OrderNumber = "LK-IPTAL",
                        CustomerName = "Ayşe",
                        OrderDate = new DateOnly(2026, 9, 10),
                        DeliveryDate = new DateOnly(2026, 9, 12),
                        DeliveryPlace = default,
                        Status = OrderStatus.Iptal,
                        ItemCount = 1,
                        TotalQuantity = 2
                    }
                ],
                TotalCount = 1,
                Page = 1,
                PageSize = 20
            });
        }

        public Task<OrderDetailDto?> GetAsync(int id, CancellationToken cancellationToken = default)
            => Task.FromResult<OrderDetailDto?>(null);

        public Task<int> CreateAsync(OrderDraft draft, CancellationToken cancellationToken = default)
            => Task.FromResult(0);

        public Task UpdateAsync(int id, OrderDraft draft, DateTime rowVersion, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task ChangeStatusAsync(int id, OrderStatus next, DateTime rowVersion, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task DeleteAsync(int id, DateTime rowVersion, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<PersonSuggestions> ListPersonSuggestionsAsync(CancellationToken cancellationToken = default)
        {
            SuggestionCalls++;
            return Task.FromResult(new PersonSuggestions(["Ayşe Yılmaz"], ["Ayşe Yılmaz"]));
        }
    }
}
