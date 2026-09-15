using System.Net.Http.Headers;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using LedKasa.Siparis.Common;
using LedKasa.Siparis.Features.Orders.Domain;
using Microsoft.Extensions.Options;

namespace LedKasa.Siparis.Features.Notifications;

public interface IWhatsAppNotifier
{
    Task NotifyOrderCreatedAsync(WhatsAppOrderNotice order, CancellationToken cancellationToken = default);
}

public sealed class WhatsAppNotifier : IWhatsAppNotifier
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly HttpClient _http;
    private readonly WhatsAppOptions _options;
    private readonly ILogger<WhatsAppNotifier> _logger;

    public WhatsAppNotifier(
        HttpClient http,
        IOptions<WhatsAppOptions> options,
        ILogger<WhatsAppNotifier> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    public async Task NotifyOrderCreatedAsync(WhatsAppOrderNotice order, CancellationToken cancellationToken = default)
    {
        var token = _options.TrimmedToken;
        var phoneNumberId = _options.TrimmedPhoneNumberId;
        var recipients = WhatsAppPhone.ParseRecipients(_options.Recipients);
        if (token is null || phoneNumberId is null || recipients.Count == 0)
        {
            _logger.LogWarning(
                "WhatsApp bildirimi atlandı: AccessToken, PhoneNumberId veya Recipients yok. Sipariş {OrderNumber}",
                order.OrderNumber);
            return;
        }

        var text = WhatsAppMessageFormatter.FormatOrderCreated(order, _options.AppUrl);
        foreach (var to in recipients)
            await SendAsync(token, phoneNumberId, to, order, text, cancellationToken);
    }

    private async Task SendAsync(
        string token,
        string phoneNumberId,
        string to,
        WhatsAppOrderNotice order,
        string text,
        CancellationToken cancellationToken)
    {
        try
        {
            var payload = BuildPayload(to, order, text);
            var path = $"{_options.TrimmedApiVersion}/{phoneNumberId}/messages";
            using var request = new HttpRequestMessage(HttpMethod.Post, path)
            {
                Content = new StringContent(JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using var response = await _http.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning(
                    "WhatsApp bildirimi başarısız: {Status} {To} {Body}",
                    (int)response.StatusCode,
                    to,
                    body);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "WhatsApp bildirimi gönderilemedi: {OrderNumber} {To}", order.OrderNumber, to);
        }
    }

    private CloudApiMessage BuildPayload(string to, WhatsAppOrderNotice order, string text)
    {
        var templateName = _options.TrimmedTemplateName;
        if (templateName is null)
        {
            return new CloudApiMessage
            {
                To = to,
                Type = "text",
                Text = new CloudApiText { Body = text }
            };
        }

        var delivery = $"{TurkeyTime.FormatDate(order.DeliveryDate)} · {DisplayNames.Place(order.Place)}";
        var link = WhatsAppMessageFormatter.BuildOrderUrl(_options.AppUrl, order.Id);
        return new CloudApiMessage
        {
            To = to,
            Type = "template",
            Template = new CloudApiTemplate
            {
                Name = templateName,
                Language = new CloudApiLanguage { Code = _options.TrimmedTemplateLanguage },
                Components =
                [
                    new CloudApiComponent
                    {
                        Type = "body",
                        Parameters =
                        [
                            TextParam(order.OrderNumber),
                            TextParam(order.CustomerName),
                            TextParam(delivery),
                            TextParam(string.IsNullOrWhiteSpace(link) ? "-" : link)
                        ]
                    }
                ]
            }
        };
    }

    private static CloudApiParameter TextParam(string? value) =>
        new() { Type = "text", Text = WhatsAppMessageFormatter.TemplateParam(value) };

    private sealed class CloudApiMessage
    {
        [JsonPropertyName("messaging_product")]
        public string MessagingProduct { get; init; } = "whatsapp";

        [JsonPropertyName("to")]
        public required string To { get; init; }

        [JsonPropertyName("type")]
        public required string Type { get; init; }

        [JsonPropertyName("text")]
        public CloudApiText? Text { get; init; }

        [JsonPropertyName("template")]
        public CloudApiTemplate? Template { get; init; }
    }

    private sealed class CloudApiText
    {
        [JsonPropertyName("preview_url")]
        public bool PreviewUrl { get; init; }

        [JsonPropertyName("body")]
        public required string Body { get; init; }
    }

    private sealed class CloudApiTemplate
    {
        [JsonPropertyName("name")]
        public required string Name { get; init; }

        [JsonPropertyName("language")]
        public required CloudApiLanguage Language { get; init; }

        [JsonPropertyName("components")]
        public required IReadOnlyList<CloudApiComponent> Components { get; init; }
    }

    private sealed class CloudApiLanguage
    {
        [JsonPropertyName("code")]
        public required string Code { get; init; }
    }

    private sealed class CloudApiComponent
    {
        [JsonPropertyName("type")]
        public required string Type { get; init; }

        [JsonPropertyName("parameters")]
        public required IReadOnlyList<CloudApiParameter> Parameters { get; init; }
    }

    private sealed class CloudApiParameter
    {
        [JsonPropertyName("type")]
        public required string Type { get; init; }

        [JsonPropertyName("text")]
        public required string Text { get; init; }
    }
}
