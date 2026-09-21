using Bunit;
using Bunit.TestDoubles;
using FluentAssertions;
using LedKasa.Siparis.Components.Pages;
using LedKasa.Siparis.Features.Dashboard;
using LedKasa.Siparis.Features.Orders;
using LedKasa.Siparis.Features.Orders.Domain;
using LedKasa.Siparis.Identity;
using LedKasa.Siparis.Security;
using LedKasa.Siparis.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;

namespace LedKasa.Siparis.Tests.Components;

public class DashboardStatusLinksTests : TestContext
{
    public DashboardStatusLinksTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
        Services.AddLogging();
        var auth = this.AddTestAuthorization();
        auth.SetAuthorized("admin");
        auth.SetPolicies(Policies.OrdersView);
        auth.SetRoles(AppRoles.Yonetici);
        Services.AddSingleton<ICurrentUser>(new TestCurrentUser());
        Services.AddSingleton<IDashboardService>(new FakeDashboard());
    }

    [Fact]
    public void StatusRows_ShouldLinkToOrdersWithStatusQuery()
    {
        var cut = RenderComponent<Dashboard>();

        cut.FindAll("a.lk-status-row--link")
            .Select(a => a.GetAttribute("href"))
            .Should().BeEquivalentTo(
                $"/orders?status={(int)OrderStatus.Yeni}",
                $"/orders?status={(int)OrderStatus.Onaylandi}",
                $"/orders?status={(int)OrderStatus.Iptal}");
    }

    [Fact]
    public void StatCards_ShouldLinkToOrdersWithScope()
    {
        var cut = RenderComponent<Dashboard>();

        cut.FindAll("a.lk-stat-link")
            .Select(a => a.GetAttribute("href"))
            .Should().Equal(
                $"/orders?scope={OrderScopes.TodayQuery}",
                $"/orders?scope={OrderScopes.OpenQuery}",
                $"/orders?scope={OrderScopes.WeekQuery}",
                $"/orders?scope={OrderScopes.OverdueQuery}");
    }

    private sealed class FakeDashboard : IDashboardService
    {
        public Task<DashboardSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new DashboardSnapshot
            {
                StatusCounts =
                [
                    new StatusCount(OrderStatus.Yeni, 7),
                    new StatusCount(OrderStatus.Onaylandi, 5),
                    new StatusCount(OrderStatus.Iptal, 2)
                ]
            });
    }
}
