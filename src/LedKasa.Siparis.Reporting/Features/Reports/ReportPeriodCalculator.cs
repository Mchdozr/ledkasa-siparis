using LedKasa.Siparis.Common;

namespace LedKasa.Siparis.Features.Reports;

public static class ReportPeriodCalculator
{
    public static (DateOnly From, DateOnly To) GetRange(ReportPeriod period, DateOnly anchor)
    {
        return period switch
        {
            ReportPeriod.Daily => (anchor, anchor),
            ReportPeriod.Weekly => IsoWeekRange.Containing(anchor),
            ReportPeriod.Monthly => (new DateOnly(anchor.Year, anchor.Month, 1),
                new DateOnly(anchor.Year, anchor.Month, DateTime.DaysInMonth(anchor.Year, anchor.Month))),
            ReportPeriod.Yearly => (new DateOnly(anchor.Year, 1, 1), new DateOnly(anchor.Year, 12, 31)),
            _ => throw new ArgumentOutOfRangeException(nameof(period), period, null)
        };
    }
}
