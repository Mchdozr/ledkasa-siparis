namespace LedKasa.Siparis.Features.Orders.Domain;

public sealed class OrderItem
{
    private readonly List<OrderItemExtraFeature> _extraFeatures = [];

    public int Id { get; private set; }
    public int OrderId { get; private set; }
    public int ProductId { get; private set; }
    public Product? Product { get; private set; }
    public decimal WidthCm { get; private set; }
    public decimal HeightCm { get; private set; }
    public int Quantity { get; private set; }
    public decimal LineTotal { get; private set; }
    public string? Note { get; private set; }
    public IReadOnlyCollection<OrderItemExtraFeature> ExtraFeatures => _extraFeatures;

    private OrderItem()
    {
    }

    public static OrderItem Create(
        int productId,
        decimal widthCm,
        decimal heightCm,
        int quantity,
        decimal lineTotal,
        IEnumerable<int>? extraFeatureIds = null,
        string? note = null)
    {
        if (productId <= 0)
            throw new DomainException("Ürün seçimi zorunludur.");
        if (widthCm <= 0)
            throw new DomainException("Yatay ölçü sıfırdan büyük olmalıdır.");
        if (heightCm <= 0)
            throw new DomainException("Dikey ölçü sıfırdan büyük olmalıdır.");
        if (quantity <= 0)
            throw new DomainException("Adet sıfırdan büyük olmalıdır.");
        if (lineTotal < 0)
            throw new DomainException("Tutar negatif olamaz.");

        var item = new OrderItem
        {
            ProductId = productId,
            WidthCm = decimal.Round(widthCm, 2, MidpointRounding.AwayFromZero),
            HeightCm = decimal.Round(heightCm, 2, MidpointRounding.AwayFromZero),
            Quantity = quantity,
            LineTotal = decimal.Round(lineTotal, 2, MidpointRounding.AwayFromZero),
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim()
        };

        var distinctIds = (extraFeatureIds ?? []).Where(id => id > 0).Distinct().ToList();
        foreach (var featureId in distinctIds)
            item._extraFeatures.Add(new OrderItemExtraFeature(item, featureId));

        return item;
    }

    internal void AttachTo(Order order)
    {
        ArgumentNullException.ThrowIfNull(order);
        OrderId = order.Id;
    }
}
