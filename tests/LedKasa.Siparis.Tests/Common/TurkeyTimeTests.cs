using FluentAssertions;
using LedKasa.Siparis.Common;

namespace LedKasa.Siparis.Tests.Common;

public class TurkeyTimeTests
{
    [Fact]
    public void FormatDate_ShouldUseTurkishFormat()
    {
        TurkeyTime.FormatDate(new DateOnly(2026, 9, 15)).Should().Be("15.09.2026");
    }
}
