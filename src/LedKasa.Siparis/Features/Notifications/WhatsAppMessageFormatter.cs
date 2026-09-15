using System.Text;
using LedKasa.Siparis.Common;
using LedKasa.Siparis.Features.Orders.Domain;

namespace LedKasa.Siparis.Features.Notifications;

public static class WhatsAppMessageFormatter
{
    public const int MaxLength = 4096;

    public static string FormatOrderCreated(WhatsAppOrderNotice order, string appUrl)
    {
        var text = new StringBuilder();
        text.AppendLine("*Yeni sipariş*");
        text.AppendLine($"📦 Sipariş no: *{Esc(order.OrderNumber)}*");
        text.AppendLine($"👤 Sipariş veren kişi: {Esc(order.CustomerName)}");
        text.AppendLine($"📅 Sipariş tarihi: {TurkeyTime.FormatDate(order.OrderDate)}");
        text.AppendLine($"🚚 Teslim tarihi: {TurkeyTime.FormatDate(order.DeliveryDate)} · Teslim yeri: {Esc(DisplayNames.Place(order.Place))}");
        if (!string.IsNullOrWhiteSpace(order.Notes))
            text.AppendLine($"📝 Not: {Esc(order.Notes)}");

        text.AppendLine();
        text.AppendLine("*Kalemler*");
        foreach (var line in order.Lines)
        {
            text.AppendLine($"Ürün: {Esc(line.ProductName)}");
            text.AppendLine($"Ölçü: {DisplayNames.Size(line.WidthCm, line.HeightCm, line.DepthCm)}");
            text.AppendLine($"Yön: {DisplayNames.Side(line.Side)}");
            text.AppendLine($"Adet: {line.Quantity}");
            if (!string.IsNullOrWhiteSpace(line.Note))
                text.AppendLine($"Not: {Esc(line.Note)}");
            text.AppendLine();
        }

        if (!string.IsNullOrWhiteSpace(order.CreatedBy))
            text.AppendLine($"✍️ Oluşturan: {Esc(order.CreatedBy)}");

        var link = BuildOrderUrl(appUrl, order.Id);
        if (!string.IsNullOrWhiteSpace(link))
            text.AppendLine($"🔗 {link}");

        var message = text.ToString().Trim();
        return message.Length <= MaxLength ? message : message[..(MaxLength - 3)] + "...";
    }

    public static string TemplateParam(string? value, int maxLength = 200)
    {
        var text = (value ?? string.Empty)
            .Replace('\r', ' ')
            .Replace('\n', ' ')
            .Replace('\t', ' ')
            .Trim();

        while (text.Contains("  ", StringComparison.Ordinal))
            text = text.Replace("  ", " ", StringComparison.Ordinal);

        if (string.IsNullOrWhiteSpace(text))
            return "-";

        return text.Length <= maxLength ? text : text[..(maxLength - 3)] + "...";
    }

    public static string BuildOrderUrl(string appUrl, int id)
    {
        if (string.IsNullOrWhiteSpace(appUrl) || id <= 0)
            return string.Empty;

        return $"{appUrl.TrimEnd('/')}/orders/{id}";
    }

    private static string Esc(string? value) =>
        (value ?? string.Empty)
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("*", "\\*", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal)
            .Replace("~", "\\~", StringComparison.Ordinal)
            .Replace("`", "\\`", StringComparison.Ordinal);
}
