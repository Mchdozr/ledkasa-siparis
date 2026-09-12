namespace LedKasa.Siparis.Features.Notifications;

public static class TelegramCommands
{
    public const string StartReply =
        "<b>LEDKASA Sipariş botu hazır.</b>\nYeni sipariş açılınca buraya bildirim gelecek.";

    public static bool IsStart(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var command = text.Trim();
        var space = command.IndexOf(' ');
        if (space > 0)
            command = command[..space];

        return command.Equals("/start", StringComparison.OrdinalIgnoreCase)
               || command.StartsWith("/start@", StringComparison.OrdinalIgnoreCase);
    }
}
