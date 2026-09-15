using FluentAssertions;
using LedKasa.Siparis.Features.Notifications;
using LedKasa.Siparis.Features.Orders.Domain;

namespace LedKasa.Siparis.Tests.Notifications;

public class WhatsAppMessageFormatterTests
{
    [Fact]
    public void FormatOrderCreated_ShouldIncludeCoreFields()
    {
        var notice = new WhatsAppOrderNotice(
            12,
            "LK-20260912-0001",
            "Ayşe *Kaya*",
            new DateOnly(2026, 9, 12),
            new DateOnly(2026, 9, 20),
            DeliveryPlace.Fabrika,
            "acele",
            "Admin",
            [
                new WhatsAppOrderLine("Rental LED Kabinet", 96, 96, 5, 4, PanelSide.CiftYon, "rental")
            ]);

        var text = WhatsAppMessageFormatter.FormatOrderCreated(notice, "https://siparis.ledkasa.com.tr");

        text.Should().Contain("Yeni sipariş");
        text.Should().Contain("Sipariş no:");
        text.Should().Contain("LK-20260912-0001");
        text.Should().Contain("Sipariş veren kişi: Ayşe \\*Kaya\\*");
        text.Should().Contain("Sipariş tarihi: 12.09.2026");
        text.Should().Contain("Teslim tarihi: 20.09.2026");
        text.Should().Contain("Teslim yeri: Fabrika");
        text.Should().Contain("Not: acele");
        text.Should().Contain("Ürün: Rental LED Kabinet");
        text.Should().Contain("Ölçü: 96 × 96 × 5 cm");
        text.Should().Contain("Yön: Çift yön");
        text.Should().Contain("Adet: 4");
        text.Should().Contain("Oluşturan: Admin");
        text.Should().Contain("https://siparis.ledkasa.com.tr/orders/12");
    }

    [Fact]
    public void TemplateParam_ShouldCollapseWhitespace()
    {
        WhatsAppMessageFormatter.TemplateParam("  Ayşe\nKaya  ").Should().Be("Ayşe Kaya");
        WhatsAppMessageFormatter.TemplateParam("   ").Should().Be("-");
    }
}
