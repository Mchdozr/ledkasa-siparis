using System.Text.Json;

namespace LedKasa.Siparis.Features.Notifications;

public readonly record struct TelegramChatSignal(string ChatId);

public static class TelegramUpdateReader
{
    public static IReadOnlyList<TelegramChatSignal> Read(string json)
    {
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("result", out var result) || result.ValueKind != JsonValueKind.Array)
            return [];

        var signals = new List<TelegramChatSignal>();
        foreach (var update in result.EnumerateArray())
        {
            if (TryReadCommand(update, out var commandChat))
                signals.Add(commandChat);
            else if (TryReadAdded(update, out var addedChat))
                signals.Add(addedChat);
        }

        return signals;
    }

    public static long NextOffset(string json, long offset)
    {
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("result", out var result) || result.ValueKind != JsonValueKind.Array)
            return offset;

        foreach (var update in result.EnumerateArray())
        {
            if (update.TryGetProperty("update_id", out var id))
                offset = Math.Max(offset, id.GetInt64() + 1);
        }

        return offset;
    }

    private static bool TryReadCommand(JsonElement update, out TelegramChatSignal signal)
    {
        signal = default;
        if (!update.TryGetProperty("message", out var message))
            return false;

        var text = message.TryGetProperty("text", out var textEl) ? textEl.GetString() : null;
        if (!TelegramCommands.IsChatCommand(text))
            return false;

        if (!TryChatId(message, out var chatId))
            return false;

        signal = new TelegramChatSignal(chatId);
        return true;
    }

    private static bool TryReadAdded(JsonElement update, out TelegramChatSignal signal)
    {
        signal = default;
        if (!update.TryGetProperty("my_chat_member", out var member))
            return false;

        var oldStatus = member.TryGetProperty("old_chat_member", out var oldMember)
            ? oldMember.GetProperty("status").GetString()
            : null;
        var newStatus = member.TryGetProperty("new_chat_member", out var newMember)
            ? newMember.GetProperty("status").GetString()
            : null;

        if (!WasAdded(oldStatus, newStatus) || !TryChatId(member, out var chatId))
            return false;

        signal = new TelegramChatSignal(chatId);
        return true;
    }

    private static bool TryChatId(JsonElement container, out string chatId)
    {
        chatId = string.Empty;
        if (!container.TryGetProperty("chat", out var chat) || !chat.TryGetProperty("id", out var id))
            return false;

        chatId = id.GetRawText();
        return !string.IsNullOrWhiteSpace(chatId);
    }

    private static bool WasAdded(string? oldStatus, string? newStatus)
    {
        var joined = newStatus is "member" or "administrator";
        var fromOut = oldStatus is null or "left" or "kicked";
        return joined && fromOut;
    }
}
