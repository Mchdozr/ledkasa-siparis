namespace LedKasa.Siparis.Features.Notifications;

public static class WhatsAppPhone
{
    public static IReadOnlyList<string> ParseRecipients(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return [];

        return raw
            .Split([',', ';', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(Normalize)
            .OfType<string>()
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    public static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var digits = new string(value.Where(char.IsDigit).ToArray());
        if (digits.StartsWith("00", StringComparison.Ordinal))
            digits = digits[2..];

        if (digits.Length == 11 && digits.StartsWith('0'))
            digits = "90" + digits[1..];
        else if (digits.Length == 10 && digits.StartsWith('5'))
            digits = "90" + digits;

        return digits.Length is >= 10 and <= 15 ? digits : null;
    }
}
