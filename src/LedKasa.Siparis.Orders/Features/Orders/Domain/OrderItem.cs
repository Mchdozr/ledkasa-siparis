namespace LedKasa.Siparis.Features.Orders.Domain;

public sealed class OrderItem
{
    public int Id { get; private set; }
    public int OrderId { get; private set; }
    public int ProductId { get; private set; }
    public Product? Product { get; private set; }
    public int WidthCm { get; private set; }
    public int HeightCm { get; private set; }
    public int DepthCm { get; private set; }
    public int Quantity { get; private set; }
    public PanelSide Side { get; private set; }
    public string? Note { get; private set; }

    private OrderItem()
    {
    }

    public static OrderItem Create(
        int productId,
        int widthCm,
        int heightCm,
        int depthCm,
        int quantity,
        PanelSide side,
        string? note = null)
    {
        if (productId <= 0)
            throw new DomainException("Ürün seçimi zorunludur.");
        if (widthCm <= 0)
            throw new DomainException("Yatay ölçü sıfırdan büyük olmalıdır.");
        if (heightCm <= 0)
            throw new DomainException("Dikey ölçü sıfırdan büyük olmalıdır.");
        if (depthCm <= 0)
            throw new DomainException("Derinlik / kalınlık sıfırdan büyük olmalıdır.");
        if (quantity <= 0)
            throw new DomainException("Adet sıfırdan büyük olmalıdır.");
        if (!Enum.IsDefined(side))
            throw new DomainException("Yön geçersiz.");

        return new OrderItem
        {
            ProductId = productId,
            WidthCm = widthCm,
            HeightCm = heightCm,
            DepthCm = depthCm,
            Quantity = quantity,
            Side = side,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim()
        };
    }

    internal void AttachTo(Order order)
    {
        ArgumentNullException.ThrowIfNull(order);
        OrderId = order.Id;
    }
}
