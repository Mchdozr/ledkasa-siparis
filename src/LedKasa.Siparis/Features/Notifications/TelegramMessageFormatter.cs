using System.Text;
using LedKasa.Siparis.Common;
using LedKasa.Siparis.Features.Orders.Domain;

namespace LedKasa.Siparis.Features.Notifications;

public static class TelegramMessageFormatter
{
    public static string FormatOrderCreated(TelegramOrderNotice order, string appUrl)
    {
        var text = new StringBuilder();
        text.AppendLine("<b>Yeni sipariş</b>");
        text.AppendLine($"📦 {Bold(order.OrderNumber)}");
        text.AppendLine($"👤 {Esc(order.CustomerName)}");
        text.AppendLine($"📅 Sipariş: {TurkeyTime.FormatDate(order.OrderDate)}");
        text.AppendLine($"🚚 Teslim: {TurkeyTime.FormatDate(order.DeliveryDate)} · {Esc(DisplayNames.Place(order.Place))}");
        text.AppendLine($"📍 {Esc(order.DeliveryAddress)}");
        if (!string.IsNullOrWhiteSpace(order.Notes))
            text.AppendLine($"📝 {Esc(order.Notes)}");

        text.AppendLine();
        text.AppendLine("<b>Kalemler</b>");
        foreach (var line in order.Lines)
        {
            text.AppendLine($"• {Esc(line.ProductName)} {FormatCm(line.WidthCm)}x{FormatCm(line.HeightCm)} cm × {line.Quantity}");
            if (line.Extras.Count > 0)
                text.AppendLine($"  + {Esc(string.Join(", ", line.Extras))}");
            if (!string.IsNullOrWhiteSpace(line.Note))
                text.AppendLine($"  not: {Esc(line.Note)}");
            text.AppendLine($"  {Esc(TurkeyTime.FormatMoney(line.LineTotal, order.Currency))}");
        }

        text.AppendLine();
        text.AppendLine($"💰 Toplam: {Bold(TurkeyTime.FormatMoney(order.GrandTotal, order.Currency))}");
        if (!string.IsNullOrWhiteSpace(order.CreatedBy))
            text.AppendLine($"✍️ {Esc(order.CreatedBy)}");

        var link = BuildOrderUrl(appUrl, order.Id);
        if (!string.IsNullOrWhiteSpace(link))
            text.AppendLine($"🔗 {link}");

        var message = text.ToString().Trim();
        return message.Length <= 4000 ? message : message[..3997] + "...";
    }

    private static string BuildOrderUrl(string appUrl, int id)
    {
        if (string.IsNullOrWhiteSpace(appUrl) || id <= 0)
            return string.Empty;

        return $"{appUrl.TrimEnd('/')}/orders/{id}";
    }

    private static string FormatCm(decimal value) => value.ToString("0.##", TurkeyTime.Culture);

    private static string Esc(string? value) =>
        (value ?? string.Empty)
            .Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal);

    private static string Bold(string value) => $"<b>{Esc(value)}</b>";
}
