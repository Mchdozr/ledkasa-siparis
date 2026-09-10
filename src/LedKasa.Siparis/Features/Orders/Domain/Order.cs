namespace LedKasa.Siparis.Features.Orders.Domain;

public sealed class Order
{
    private readonly List<OrderItem> _items = [];
    private OrderStatus _status = OrderStatus.Yeni;

    public int Id { get; private set; }
    public string OrderNumber { get; private set; } = string.Empty;
    public string CustomerName { get; private set; } = string.Empty;
    public DateOnly OrderDate { get; private set; }
    public DateOnly DeliveryDate { get; private set; }
    public DeliveryPlace DeliveryPlace { get; private set; }
    public OrderStatus Status => _status;
    public string? Notes { get; private set; }
    public string CreatedByUserId { get; private set; } = string.Empty;
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }
    public string? UpdatedByUserId { get; private set; }
    public decimal GrandTotal { get; private set; }
    public DateTime RowVersion { get; private set; }
    public IReadOnlyCollection<OrderItem> Items => _items;

    private Order()
    {
    }

    public static Order Create(
        string orderNumber,
        string customerName,
        DateOnly orderDate,
        DateOnly deliveryDate,
        DeliveryPlace deliveryPlace,
        IEnumerable<OrderItem> items,
        string createdByUserId,
        string? notes = null,
        DateTime? nowUtc = null)
    {
        if (string.IsNullOrWhiteSpace(orderNumber))
            throw new DomainException("Sipariş numarası zorunludur.");
        if (string.IsNullOrWhiteSpace(customerName))
            throw new DomainException("Sipariş veren kişi zorunludur.");
        if (string.IsNullOrWhiteSpace(createdByUserId))
            throw new DomainException("Oluşturan kullanıcı zorunludur.");
        if (!Enum.IsDefined(deliveryPlace))
            throw new DomainException("Teslim yeri geçersiz.");
        if (deliveryDate < orderDate)
            throw new DomainException("Teslim tarihi sipariş tarihinden önce olamaz.");

        var itemList = items?.ToList() ?? [];
        if (itemList.Count == 0)
            throw new DomainException("Siparişte en az bir kalem olmalıdır.");

        var order = new Order
        {
            OrderNumber = orderNumber.Trim(),
            CustomerName = customerName.Trim(),
            OrderDate = orderDate,
            DeliveryDate = deliveryDate,
            DeliveryPlace = deliveryPlace,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            CreatedByUserId = createdByUserId,
            CreatedAtUtc = nowUtc ?? DateTime.UtcNow
        };
        order._status = OrderStatus.Yeni;
        order.ReplaceItemCollection(itemList);
        return order;
    }

    public void UpdateHeader(
        string customerName,
        DateOnly orderDate,
        DateOnly deliveryDate,
        DeliveryPlace deliveryPlace,
        string? notes,
        string updatedByUserId,
        DateTime? nowUtc = null)
    {
        EnsureEditable();
        if (string.IsNullOrWhiteSpace(customerName))
            throw new DomainException("Sipariş veren kişi zorunludur.");
        if (!Enum.IsDefined(deliveryPlace))
            throw new DomainException("Teslim yeri geçersiz.");
        if (deliveryDate < orderDate)
            throw new DomainException("Teslim tarihi sipariş tarihinden önce olamaz.");
        if (string.IsNullOrWhiteSpace(updatedByUserId))
            throw new DomainException("Güncelleyen kullanıcı zorunludur.");

        CustomerName = customerName.Trim();
        OrderDate = orderDate;
        DeliveryDate = deliveryDate;
        DeliveryPlace = deliveryPlace;
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        Touch(updatedByUserId, nowUtc);
    }

    public void ReplaceItems(IEnumerable<OrderItem> items, string updatedByUserId, DateTime? nowUtc = null)
    {
        EnsureEditable();
        ReplaceItemCollection(items?.ToList() ?? []);
        Touch(updatedByUserId, nowUtc);
    }

    public void TransitionTo(OrderStatus next, string updatedByUserId, DateTime? nowUtc = null)
    {
        if (string.IsNullOrWhiteSpace(updatedByUserId))
            throw new DomainException("Güncelleyen kullanıcı zorunludur.");
        if (!OrderStatusTransitions.CanTransition(_status, next))
            throw new DomainException($"Geçersiz durum geçişi: {DisplayNames.Status(_status)} → {DisplayNames.Status(next)}.");

        _status = next;
        Touch(updatedByUserId, nowUtc);
    }

    public bool CanEditDetails => _status is OrderStatus.Yeni or OrderStatus.Onaylandi;

    private void EnsureEditable()
    {
        if (!CanEditDetails)
            throw new DomainException("Bu durumdaki siparişin bilgileri düzenlenemez.");
    }

    private void ReplaceItemCollection(List<OrderItem> items)
    {
        if (items.Count == 0)
            throw new DomainException("Siparişte en az bir kalem olmalıdır.");

        _items.Clear();
        foreach (var item in items)
        {
            item.AttachTo(this);
            _items.Add(item);
        }

        GrandTotal = decimal.Round(_items.Sum(i => i.LineTotal), 2, MidpointRounding.AwayFromZero);
    }

    private void Touch(string userId, DateTime? nowUtc)
    {
        UpdatedByUserId = userId;
        UpdatedAtUtc = nowUtc ?? DateTime.UtcNow;
    }
}
