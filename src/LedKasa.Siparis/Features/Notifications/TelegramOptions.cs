namespace LedKasa.Siparis.Features.Notifications;

public sealed class TelegramOptions
{
    public const string Section = "Telegram";

    public string? BotToken { get; set; }
    public string? ChatId { get; set; }
    public string AppUrl { get; set; } = "https://siparis.ledkasa.com.tr";

    public string? TrimmedToken => string.IsNullOrWhiteSpace(BotToken) ? null : BotToken.Trim();
    public string? TrimmedChatId => string.IsNullOrWhiteSpace(ChatId) ? null : ChatId.Trim();

    public bool HasBot => TrimmedToken is not null;
}

