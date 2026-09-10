using FluentAssertions;
using LedKasa.Siparis.Common;
using LedKasa.Siparis.Features.Orders.Domain;

namespace LedKasa.Siparis.Tests.Common;

public class TurkeyTimeTests
{
    [Fact]
    public void FormatMoney_ShouldDefaultToTry()
    {
        TurkeyTime.FormatMoney(1500.5m).Should().Be("1.500,50 ₺");
    }

    [Theory]
    [InlineData(Currency.Try, "1.500,50 ₺")]
    [InlineData(Currency.Usd, "1.500,50 $")]
    [InlineData(Currency.Eur, "1.500,50 €")]
    public void FormatMoney_ShouldUseCurrencySymbol(Currency currency, string expected)
    {
        TurkeyTime.FormatMoney(1500.5m, currency).Should().Be(expected);
    }

    [Fact]
    public void FormatMoneyLine_ShouldShowUnitTimesQuantity()
    {
        TurkeyTime.FormatMoneyLine(15m, 2, 30m, Currency.Try)
            .Should().Be("15,00 ₺ × 2 = 30,00 ₺");
    }
}
