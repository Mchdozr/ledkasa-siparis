using FluentAssertions;
using LedKasa.Siparis.Features.Orders.Domain;

namespace LedKasa.Siparis.Tests.Domain;

public class DisplayNamesTests
{
    [Fact]
    public void Place_ShouldNotThrow_ForUnknownValue()
    {
        DisplayNames.Place(default).Should().Be("—");
        DisplayNames.Place((DeliveryPlace)99).Should().Be("—");
    }

    [Fact]
    public void Status_ShouldNotThrow_ForUnknownValue()
    {
        DisplayNames.Status(default).Should().Be("—");
        DisplayNames.Status((OrderStatus)99).Should().Be("—");
    }

    [Fact]
    public void Status_ShouldMapIptal()
    {
        DisplayNames.Status(OrderStatus.Iptal).Should().Be("İptal");
    }
}
