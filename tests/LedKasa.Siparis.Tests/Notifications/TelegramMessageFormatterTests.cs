using FluentAssertions;
using LedKasa.Siparis.Features.Notifications;
using LedKasa.Siparis.Features.Orders.Domain;

namespace LedKasa.Siparis.Tests.Notifications;

public class TelegramMessageFormatterTests
{
    [Fact]
    public void FormatOrderCreated_ShouldIncludeCoreFields()
    {
        var notice = new TelegramOrderNotice(
            12,
            "LK-20260912-0001",
            "Ayşe <Kaya>",
            new DateOnly(2026, 9, 12),
            new DateOnly(2026, 9, 20),
            DeliveryPlace.Fabrika,
            "OSB 2. Cad.",
            "acele",
            Currency.Usd,
            400,
            "Admin",
            [
                new TelegramOrderLine("Rental LED Kabinet", 96, 96, 4, 100, 400, "rental", ["Köşe kesim"])
            ]);

        var text = TelegramMessageFormatter.FormatOrderCreated(notice, "https://siparis.ledkasa.com.tr");

        text.Should().Contain("Yeni sipariş");
        text.Should().Contain("LK-20260912-0001");
        text.Should().Contain("Ayşe &lt;Kaya&gt;");
        text.Should().Contain("12.09.2026");
        text.Should().Contain("20.09.2026");
        text.Should().Contain("Fabrika");
        text.Should().Contain("OSB 2. Cad.");
        text.Should().Contain("acele");
        text.Should().Contain("Rental LED Kabinet");
        text.Should().Contain("96x96 cm × 4");
        text.Should().Contain("Köşe kesim");
        text.Should().Contain("400,00 $");
        text.Should().Contain("https://siparis.ledkasa.com.tr/orders/12");
    }
}
