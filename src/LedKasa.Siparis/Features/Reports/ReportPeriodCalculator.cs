namespace LedKasa.Siparis.Features.Reports;

public static class ReportPeriodCalculator
{
    public static (DateOnly From, DateOnly To) GetRange(ReportPeriod period, DateOnly anchor)
    {
        return period switch
        {
            ReportPeriod.Daily => (anchor, anchor),
            ReportPeriod.Weekly => GetIsoWeek(anchor),
            ReportPeriod.Monthly => (new DateOnly(anchor.Year, anchor.Month, 1),
                new DateOnly(anchor.Year, anchor.Month, DateTime.DaysInMonth(anchor.Year, anchor.Month))),
            ReportPeriod.Yearly => (new DateOnly(anchor.Year, 1, 1), new DateOnly(anchor.Year, 12, 31)),
            _ => throw new ArgumentOutOfRangeException(nameof(period), period, null)
        };
    }

    private static (DateOnly From, DateOnly To) GetIsoWeek(DateOnly anchor)
    {
        var daysFromMonday = ((int)anchor.DayOfWeek + 6) % 7;
        var monday = anchor.AddDays(-daysFromMonday);
        return (monday, monday.AddDays(6));
    }
}
