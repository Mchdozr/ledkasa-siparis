namespace LedKasa.Siparis.Common;

public static class IsoWeekRange
{
    public static (DateOnly From, DateOnly To) Containing(DateOnly anchor)
    {
        var daysFromMonday = ((int)anchor.DayOfWeek + 6) % 7;
        var monday = anchor.AddDays(-daysFromMonday);
        return (monday, monday.AddDays(6));
    }
}
