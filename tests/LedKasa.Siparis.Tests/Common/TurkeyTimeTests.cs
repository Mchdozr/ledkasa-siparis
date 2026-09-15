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
}
