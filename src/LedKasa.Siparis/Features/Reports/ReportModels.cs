using LedKasa.Siparis.Features.Orders.Domain;

namespace LedKasa.Siparis.Features.Reports;

public sealed class ReportRequest
{
    public ReportPeriod Period { get; set; } = ReportPeriod.Daily;
    public ReportDateField DateField { get; set; } = ReportDateField.OrderDate;
    public DateOnly Anchor { get; set; }
    public DeliveryPlace? DeliveryPlace { get; set; }
    public ICollection<OrderStatus> Statuses { get; set; } = [];
    public string? CustomerName { get; set; }
}

public sealed class ReportRow
{
    public string OrderNumber { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public DateOnly OrderDate { get; init; }
    public DateOnly DeliveryDate { get; init; }
    public string DeliveryPlace { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public int ItemCount { get; init; }
    public int TotalQuantity { get; init; }
    public decimal GrandTotal { get; init; }
    public string ItemSummary { get; init; } = string.Empty;
}

public sealed class ReportResult
{
    public DateOnly From { get; init; }
    public DateOnly To { get; init; }
    public string Title { get; init; } = string.Empty;
    public int OrderCount { get; init; }
    public int ItemCount { get; init; }
    public int TotalQuantity { get; init; }
    public decimal GrandTotal { get; init; }
    public string FormattedGrandTotal { get; init; } = string.Empty;
    public IReadOnlyList<ReportRow> Rows { get; init; } = [];
}
