namespace LedKasa.Siparis.Features.Notifications;

public sealed class TelegramRecipientHub
{
    private string? _chatId;

    public void Remember(string? chatId)
    {
        if (!string.IsNullOrWhiteSpace(chatId))
            _chatId = chatId.Trim();
    }

    public string? Current(string? configured) =>
        string.IsNullOrWhiteSpace(configured) ? _chatId : configured.Trim();
}
