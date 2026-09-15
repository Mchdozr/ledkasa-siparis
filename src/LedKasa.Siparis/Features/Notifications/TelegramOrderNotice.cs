using LedKasa.Siparis.Features.Orders.Domain;

namespace LedKasa.Siparis.Features.Notifications;

public sealed record TelegramOrderLine(
    string ProductName,
    decimal WidthCm,
    decimal HeightCm,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal,
    string? Note,
    IReadOnlyList<string> Extras);

public sealed record TelegramOrderNotice(
    int Id,
    string OrderNumber,
    string CustomerName,
    DateOnly OrderDate,
    DateOnly DeliveryDate,
    DeliveryPlace Place,
    string? Notes,
    decimal GrandTotal,
    string? CreatedBy,
    IReadOnlyList<TelegramOrderLine> Lines);
