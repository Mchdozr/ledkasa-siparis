namespace LedKasa.Siparis.Features.Notifications;

public sealed class TelegramOptions
{
    public const string Section = "Telegram";

    public string? BotToken { get; set; }
    public string? ChatId { get; set; }
    public string AppUrl { get; set; } = "https://siparis.ledkasa.com.tr";

    public bool IsEnabled =>
        !string.IsNullOrWhiteSpace(BotToken) && !string.IsNullOrWhiteSpace(ChatId);
}
