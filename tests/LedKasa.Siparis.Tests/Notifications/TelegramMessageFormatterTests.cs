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
            "acele",
            "Admin",
            [
                new TelegramOrderLine("Rental LED Kabinet", 96, 96, 4, "rental", ["Köşe kesim"])
            ]);

        var text = TelegramMessageFormatter.FormatOrderCreated(notice, "https://siparis.ledkasa.com.tr");

        text.Should().Contain("Yeni sipariş");
        text.Should().Contain("Sipariş no:");
        text.Should().Contain("LK-20260912-0001");
        text.Should().Contain("Sipariş veren kişi: Ayşe &lt;Kaya&gt;");
        text.Should().Contain("Sipariş tarihi: 12.09.2026");
        text.Should().Contain("Teslim tarihi: 20.09.2026");
        text.Should().Contain("Teslim yeri: Fabrika");
        text.Should().Contain("Not: acele");
        text.Should().Contain("Ürün: Rental LED Kabinet");
        text.Should().Contain("Ölçü: 96 × 96 cm");
        text.Should().Contain("Adet: 4");
        text.Should().Contain("Ekstra: Köşe kesim");
        text.Should().Contain("Oluşturan: Admin");
        text.Should().NotContain("Teslimat adresi");
        text.Should().NotContain("400,00");
        text.Should().NotContain("Toplam");
        text.Should().Contain("https://siparis.ledkasa.com.tr/orders/12");
    }
}
