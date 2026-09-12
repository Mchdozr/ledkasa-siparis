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
    public void ApplyQuery_ShouldPreferScopeOverStatus()
    {
        var filter = new OrderListFilter();
        OrderScopes.ApplyQuery(filter, (int)OrderStatus.Yeni, OrderScopes.OverdueQuery);
        filter.Scope.Should().Be(OrderListScope.Overdue);
        filter.Status.Should().BeNull();
    }
}
