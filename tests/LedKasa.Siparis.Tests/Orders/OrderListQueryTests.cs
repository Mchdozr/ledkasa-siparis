using FluentAssertions;
using LedKasa.Siparis.Features.Orders;
using LedKasa.Siparis.Features.Orders.Domain;

namespace LedKasa.Siparis.Tests.Orders;

public class OrderListQueryTests
{
    [Fact]
    public void ApplyQuery_ShouldSetStatus()
    {
        var filter = new OrderListFilter();
        OrderScopes.ApplyQuery(filter, (int)OrderStatus.Onaylandi, null);
        filter.Status.Should().Be(OrderStatus.Onaylandi);
        filter.Scope.Should().Be(OrderListScope.None);
    }

    [Fact]
    public void ApplyQuery_ShouldSetIptal()
    {
        var filter = new OrderListFilter();
        OrderScopes.ApplyQuery(filter, (int)OrderStatus.Iptal, null);
        filter.Status.Should().Be(OrderStatus.Iptal);
        filter.Scope.Should().Be(OrderListScope.None);
    }

    [Fact]
    public void ApplyQuery_ShouldIgnoreUnknownStatus()
    {
        var filter = new OrderListFilter { Status = OrderStatus.Yeni };
        OrderScopes.ApplyQuery(filter, 0, null);
        filter.Status.Should().BeNull();
        OrderScopes.ApplyQuery(filter, 99, null);
        filter.Status.Should().BeNull();
    }

    [Fact]
    public void ApplyQuery_ShouldPreferScopeOverStatus()
    {
        var filter = new OrderListFilter();
        OrderScopes.ApplyQuery(filter, (int)OrderStatus.Yeni, OrderScopes.OverdueQuery);
        filter.Scope.Should().Be(OrderListScope.Overdue);
        filter.Status.Should().BeNull();
    }
}
