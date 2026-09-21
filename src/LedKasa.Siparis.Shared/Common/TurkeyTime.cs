using System.Globalization;

namespace LedKasa.Siparis.Common;

public static class TurkeyTime
{
    private static readonly TimeZoneInfo Zone = ResolveZone();
    public static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("tr-TR");

    public static DateTimeOffset Now => TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, Zone);

    public static DateOnly Today => DateOnly.FromDateTime(Now.DateTime);

    public static DateTime ToTurkey(DateTime utc)
    {
        var specified = DateTime.SpecifyKind(utc, DateTimeKind.Utc);
        return TimeZoneInfo.ConvertTimeFromUtc(specified, Zone);
    }

    public static string FormatDate(DateOnly date) => date.ToString("dd.MM.yyyy");

    public static string FormatDateTime(DateTime utc) => ToTurkey(utc).ToString("dd.MM.yyyy HH:mm");

    private static TimeZoneInfo ResolveZone()
    {
        foreach (var id in new[] { "Turkey Standard Time", "Europe/Istanbul" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
            }
        }

        return TimeZoneInfo.CreateCustomTimeZone(
            "LEDKASA-TR",
            TimeSpan.FromHours(3),
            "Türkiye",
            "Türkiye");
    }
}
