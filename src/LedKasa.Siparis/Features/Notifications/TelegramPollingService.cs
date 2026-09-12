using System.Text.Json;
using Microsoft.Extensions.Options;

namespace LedKasa.Siparis.Features.Notifications;

public sealed class TelegramPollingService : BackgroundService
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly TelegramOptions _options;
    private readonly TelegramRecipientHub _recipients;
    private readonly ILogger<TelegramPollingService> _logger;

    public TelegramPollingService(
        IHttpClientFactory httpFactory,
        IOptions<TelegramOptions> options,
        TelegramRecipientHub recipients,
        ILogger<TelegramPollingService> logger)
    {
        _httpFactory = httpFactory;
        _options = options.Value;
        _recipients = recipients;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.HasBot)
            return;

        var client = _httpFactory.CreateClient("telegram-bot");
        _recipients.Remember(_options.TrimmedChatId);
        if (_recipients.Current(_options.TrimmedChatId) is { } configuredChat)
            await SendTextAsync(client, configuredChat, TelegramCommands.StartReply, stoppingToken);

        var offset = 0L;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var url = $"/bot{_options.TrimmedToken}/getUpdates?timeout=25&offset={offset}";
                using var response = await client.GetAsync(url, stoppingToken);
                var json = await response.Content.ReadAsStringAsync(stoppingToken);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Telegram getUpdates başarısız: {Status}", (int)response.StatusCode);
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                    continue;
                }

                offset = await HandleUpdatesAsync(client, json, offset, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Telegram polling hatası");
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }

    private async Task<long> HandleUpdatesAsync(HttpClient client, string json, long offset, CancellationToken stoppingToken)
    {
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("result", out var result) || result.ValueKind != JsonValueKind.Array)
            return offset;

        foreach (var update in result.EnumerateArray())
        {
            if (update.TryGetProperty("update_id", out var id))
                offset = Math.Max(offset, id.GetInt64() + 1);

            if (!update.TryGetProperty("message", out var message))
                continue;

            var text = message.TryGetProperty("text", out var textEl) ? textEl.GetString() : null;
            if (!TelegramCommands.IsStart(text))
                continue;

            var chatId = message.GetProperty("chat").GetProperty("id").GetRawText();
            _recipients.Remember(chatId);
            await SendTextAsync(client, chatId, TelegramCommands.StartReply, stoppingToken);
        }

        return offset;
    }

    private async Task SendTextAsync(HttpClient client, string chatId, string text, CancellationToken stoppingToken)
    {
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["chat_id"] = chatId,
            ["text"] = text,
            ["parse_mode"] = "HTML"
        });
        using var response = await client.PostAsync($"/bot{_options.TrimmedToken}/sendMessage", content, stoppingToken);
        if (!response.IsSuccessStatusCode)
            _logger.LogWarning("Telegram mesajı gönderilemedi: {Status}", (int)response.StatusCode);
    }
}
