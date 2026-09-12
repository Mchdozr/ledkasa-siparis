using System.Net.Http.Headers;
using Microsoft.Extensions.Options;

namespace LedKasa.Siparis.Features.Notifications;

public interface ITelegramNotifier
{
    Task NotifyOrderCreatedAsync(TelegramOrderNotice order, CancellationToken cancellationToken = default);
}

public sealed class TelegramNotifier : ITelegramNotifier
{
    private readonly HttpClient _http;
    private readonly TelegramOptions _options;
    private readonly ILogger<TelegramNotifier> _logger;

    public TelegramNotifier(HttpClient http, IOptions<TelegramOptions> options, ILogger<TelegramNotifier> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    public async Task NotifyOrderCreatedAsync(TelegramOrderNotice order, CancellationToken cancellationToken = default)
    {
        if (!_options.IsEnabled)
            return;

        var text = TelegramMessageFormatter.FormatOrderCreated(order, _options.AppUrl);
        try
        {
            using var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["chat_id"] = _options.ChatId!,
                ["text"] = text,
                ["parse_mode"] = "HTML",
                ["disable_web_page_preview"] = "true"
            });
            using var request = new HttpRequestMessage(HttpMethod.Post, $"/bot{_options.BotToken}/sendMessage")
            {
                Content = content
            };
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using var response = await _http.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Telegram bildirimi başarısız: {Status} {Body}", (int)response.StatusCode, body);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Telegram bildirimi gönderilemedi: {OrderNumber}", order.OrderNumber);
        }
    }
}
