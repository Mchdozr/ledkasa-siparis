using LedKasa.Siparis.Features.Orders.Domain;

namespace LedKasa.Siparis.Features.Orders;

public sealed class OrderItemInput
{
    public int Id { get; set; }
    public int ProductId { get; set; } = 1;
    public int WidthCm { get; set; }
    public int HeightCm { get; set; }
    public int DepthCm { get; set; } = 5;
    public int Quantity { get; set; } = 1;
    public PanelSide Side { get; set; } = PanelSide.TekYon;
    public string? Note { get; set; }
}

public sealed class OrderDraft
{
    public string CustomerName { get; set; } = string.Empty;
    public DateOnly OrderDate { get; set; }
    public DateOnly DeliveryDate { get; set; }
    public DeliveryPlace DeliveryPlace { get; set; } = DeliveryPlace.Sirket;
    public string? Notes { get; set; }
    public List<OrderItemInput> Items { get; set; } = [new()];
}

public sealed class OrderListFilter
{
    public string? Search { get; set; }
    public OrderStatus? Status { get; set; }
    public DeliveryPlace? DeliveryPlace { get; set; }
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }
    public ReportDateFieldKind DateField { get; set; } = ReportDateFieldKind.OrderDate;
    public OrderSort Sort { get; set; } = OrderSort.NewestFirst;
    public OrderListScope Scope { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public enum ReportDateFieldKind
{
    OrderDate = 1,
    DeliveryDate = 2
}

public sealed class OrderListItemDto
{
    public int Id { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public DateOnly OrderDate { get; init; }
    public DateOnly DeliveryDate { get; init; }
    public DeliveryPlace DeliveryPlace { get; init; }
    public OrderStatus Status { get; init; }
    public int ItemCount { get; init; }
    public int TotalQuantity { get; init; }
}

public sealed class PagedResult<T>
{
    public required IReadOnlyList<T> Items { get; init; }
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
}

public sealed class OrderDetailDto
{
    public int Id { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public DateOnly OrderDate { get; init; }
    public DateOnly DeliveryDate { get; init; }
    public DeliveryPlace DeliveryPlace { get; init; }
    public OrderStatus Status { get; init; }
    public string? Notes { get; init; }
    public string CreatedByUserId { get; init; } = string.Empty;
    public DateTime CreatedAtUtc { get; init; }
    public DateTime? UpdatedAtUtc { get; init; }
    public DateTime RowVersion { get; init; }
    public IReadOnlyList<OrderItemDto> Items { get; init; } = [];
    public IReadOnlyList<OrderStatus> AllowedNextStatuses { get; init; } = [];
    public bool CanEdit { get; init; }
}

public sealed class OrderItemDto
{
    public int Id { get; init; }
    public int ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public int WidthCm { get; init; }
    public int HeightCm { get; init; }
    public int DepthCm { get; init; }
    public int Quantity { get; init; }
    public PanelSide Side { get; init; }
    public string? Note { get; init; }
}
