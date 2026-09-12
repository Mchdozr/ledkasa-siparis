using System.Text.Json;
using Microsoft.Extensions.Options;

namespace LedKasa.Siparis.Features.Notifications;

public sealed class TelegramPollingService : BackgroundService
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly TelegramOptions _options;
    private readonly ILogger<TelegramPollingService> _logger;

    public TelegramPollingService(
        IHttpClientFactory httpFactory,
        IOptions<TelegramOptions> options,
        ILogger<TelegramPollingService> logger)
    {
        _httpFactory = httpFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (string.IsNullOrWhiteSpace(_options.BotToken))
            return;

        var client = _httpFactory.CreateClient("telegram-bot");
        var offset = 0L;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var url = $"/bot{_options.BotToken}/getUpdates?timeout=25&offset={offset}";
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
            await SendStartReplyAsync(client, chatId, stoppingToken);
        }

        return offset;
    }

    private async Task SendStartReplyAsync(HttpClient client, string chatId, CancellationToken stoppingToken)
    {
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["chat_id"] = chatId,
            ["text"] = TelegramCommands.StartReply,
            ["parse_mode"] = "HTML"
        });
        using var response = await client.PostAsync($"/bot{_options.BotToken}/sendMessage", content, stoppingToken);
        if (!response.IsSuccessStatusCode)
            _logger.LogWarning("Telegram /start yanıtı gönderilemedi: {Status}", (int)response.StatusCode);
    }
}
