namespace LedKasa.Siparis.Features.Notifications;

public static class TelegramCommands
{
    public const string StartReply =
        "<b>LEDKASA Sipariş botu hazır.</b>\nYeni sipariş açılınca buraya bildirim gelecek.";

    public static string ReadyReply(string chatId) =>
        $"{StartReply}\n\nChat id: <code>{chatId}</code>\nBunu Plesk'te Telegram__ChatId olarak kaydet.";

    public static bool IsStart(string? text) => IsCommand(text, "/start");

    public static bool IsId(string? text) => IsCommand(text, "/id");

    public static bool IsChatCommand(string? text) => IsStart(text) || IsId(text);

    private static bool IsCommand(string? text, string command)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var value = text.Trim();
        var space = value.IndexOf(' ');
        if (space > 0)
            value = value[..space];

        return value.Equals(command, StringComparison.OrdinalIgnoreCase)
               || value.StartsWith(command + "@", StringComparison.OrdinalIgnoreCase);
    }
}
