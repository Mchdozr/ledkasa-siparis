namespace LedKasa.Siparis.Features.Orders.Domain;

public static class OrderStatusTransitions
{
    private static readonly Dictionary<OrderStatus, OrderStatus[]> Map = new()
    {
        [OrderStatus.Yeni] = [OrderStatus.Onaylandi, OrderStatus.Iptal],
        [OrderStatus.Onaylandi] = [OrderStatus.Uretimde, OrderStatus.Iptal],
        [OrderStatus.Uretimde] = [OrderStatus.Hazir, OrderStatus.Iptal],
        [OrderStatus.Hazir] = [OrderStatus.TeslimEdildi, OrderStatus.Iptal],
        [OrderStatus.TeslimEdildi] = [],
        [OrderStatus.Iptal] = []
    };

    public static bool CanTransition(OrderStatus from, OrderStatus to)
    {
        return Map.TryGetValue(from, out var allowed) && allowed.Contains(to);
    }

    public static IReadOnlyList<OrderStatus> AllowedFrom(OrderStatus from)
    {
        return Map.TryGetValue(from, out var allowed) ? allowed : [];
    }
}
