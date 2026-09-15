using LedKasa.Siparis.Features.Orders.Domain;

namespace LedKasa.Siparis.Features.Notifications;

public sealed record WhatsAppOrderLine(
    string ProductName,
    int WidthCm,
    int HeightCm,
    int DepthCm,
    int Quantity,
    PanelSide Side,
    string? Note);

public sealed record WhatsAppOrderNotice(
    int Id,
    string OrderNumber,
    string CustomerName,
    DateOnly OrderDate,
    DateOnly DeliveryDate,
    DeliveryPlace Place,
    string? Notes,
    string? CreatedBy,
    IReadOnlyList<WhatsAppOrderLine> Lines);
