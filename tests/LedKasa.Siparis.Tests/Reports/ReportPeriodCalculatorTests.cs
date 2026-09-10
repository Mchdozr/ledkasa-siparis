using FluentAssertions;
using LedKasa.Siparis.Features.Reports;

namespace LedKasa.Siparis.Tests.Reports;

public class ReportPeriodCalculatorTests
{
    [Fact]
    public void Daily_ShouldReturnSameDay()
    {
        var anchor = new DateOnly(2026, 9, 10);
        ReportPeriodCalculator.GetRange(ReportPeriod.Daily, anchor)
            .Should().Be((anchor, anchor));
    }

    [Fact]
    public void Weekly_ShouldBeMondayToSunday()
    {
        var thursday = new DateOnly(2026, 9, 10);
        var (from, to) = ReportPeriodCalculator.GetRange(ReportPeriod.Weekly, thursday);

        from.Should().Be(new DateOnly(2026, 9, 7));
        to.Should().Be(new DateOnly(2026, 9, 13));
        from.DayOfWeek.Should().Be(DayOfWeek.Monday);
        to.DayOfWeek.Should().Be(DayOfWeek.Sunday);
    }

    [Fact]
    public void Monthly_ShouldCoverFullMonth()
    {
        var (from, to) = ReportPeriodCalculator.GetRange(ReportPeriod.Monthly, new DateOnly(2026, 2, 10));
        from.Should().Be(new DateOnly(2026, 2, 1));
        to.Should().Be(new DateOnly(2026, 2, 28));
    }

    [Fact]
    public void Yearly_ShouldCoverFullYear()
    {
        var (from, to) = ReportPeriodCalculator.GetRange(ReportPeriod.Yearly, new DateOnly(2026, 9, 10));
        from.Should().Be(new DateOnly(2026, 1, 1));
        to.Should().Be(new DateOnly(2026, 12, 31));
    }
}
