using FluentAssertions;
using LedKasa.Siparis.Common;

namespace LedKasa.Siparis.Tests.Common;

public class TurkeyTimeTests
{
    [Fact]
    public void FormatMoney_ShouldUseTurkishNumberFormat()
    {
        TurkeyTime.FormatMoney(1500.5m).Should().Be("1.500,50");
    }

    [Fact]
    public void FormatMoneyLine_ShouldShowUnitTimesQuantity()
    {
        TurkeyTime.FormatMoneyLine(15m, 2, 30m)
            .Should().Be("15,00 × 2 = 30,00");
    }
}
