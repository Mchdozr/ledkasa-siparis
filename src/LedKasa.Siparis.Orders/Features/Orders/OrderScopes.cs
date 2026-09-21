using LedKasa.Siparis.Common;
using LedKasa.Siparis.Features.Orders.Domain;

namespace LedKasa.Siparis.Features.Orders;

public enum OrderListScope
{
    None = 0,
    Today = 1,
    Open = 2,
    WeekDelivery = 3,
    Overdue = 4
}

public static class OrderScopes
{
    public const string TodayQuery = "today";
    public const string OpenQuery = "open";
    public const string WeekQuery = "week";
    public const string OverdueQuery = "overdue";

    public static readonly OrderStatus[] Open =
    [
        OrderStatus.Yeni, OrderStatus.Onaylandi, OrderStatus.Uretimde, OrderStatus.Hazir
    ];

    public static OrderListScope Parse(string? value) => value switch
    {
        TodayQuery => OrderListScope.Today,
        OpenQuery => OrderListScope.Open,
        WeekQuery => OrderListScope.WeekDelivery,
        OverdueQuery => OrderListScope.Overdue,
        _ => OrderListScope.None
    };

    public static string Display(OrderListScope scope) => scope switch
    {
        OrderListScope.None => string.Empty,
        OrderListScope.Today => "Bugünün siparişleri",
        OrderListScope.Open => "Açık siparişler",
        OrderListScope.WeekDelivery => "Bu hafta teslimler",
        OrderListScope.Overdue => "Geciken siparişler",
        _ => throw Unexpected(scope)
    };

    public static void ApplyQuery(OrderListFilter filter, int? statusQuery, string? scopeQuery)
    {
        filter.Scope = Parse(scopeQuery);
        if (statusQuery is int value && Enum.IsDefined(typeof(OrderStatus), value))
            filter.Status = (OrderStatus)value;
        else
            filter.Status = null;

        if (filter.Scope != OrderListScope.None)
            filter.Status = null;
    }

    public static IQueryable<Order> Apply(IQueryable<Order> query, OrderListScope scope, DateOnly today)
    {
        return scope switch
        {
            OrderListScope.None => query,
            OrderListScope.Today => query.Where(o => o.OrderDate == today),
            OrderListScope.Open => query.Where(o => Open.Contains(o.Status)),
            OrderListScope.WeekDelivery => ApplyWeek(query, today),
            OrderListScope.Overdue => query.Where(o => o.DeliveryDate < today && Open.Contains(o.Status)),
            _ => throw Unexpected(scope)
        };
    }

    private static IQueryable<Order> ApplyWeek(IQueryable<Order> query, DateOnly today)
    {
        var week = IsoWeekRange.Containing(today);
        return query.Where(o =>
            o.DeliveryDate >= week.From &&
            o.DeliveryDate <= week.To &&
            o.Status != OrderStatus.Iptal);
    }

    private static ArgumentOutOfRangeException Unexpected(OrderListScope scope)
    {
        OrderListScope unused = scope;
        return new ArgumentOutOfRangeException(nameof(scope), unused, null);
    }
}
