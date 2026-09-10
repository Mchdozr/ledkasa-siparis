namespace LedKasa.Siparis.Features.Orders.Domain;

public sealed class OrderItemExtraFeature
{
    public int OrderItemId { get; private set; }
    public int ExtraFeatureId { get; private set; }
    public OrderItem? OrderItem { get; private set; }
    public ExtraFeature? ExtraFeature { get; private set; }

    private OrderItemExtraFeature()
    {
    }

    internal OrderItemExtraFeature(OrderItem item, int extraFeatureId)
    {
        OrderItem = item;
        ExtraFeatureId = extraFeatureId;
    }
}
