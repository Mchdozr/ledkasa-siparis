using FluentAssertions;
using LedKasa.Siparis.Features.Orders.Domain;

namespace LedKasa.Siparis.Tests.Domain;

public class OrderTests
{
    [Fact]
    public void Create_ShouldReject_DeliveryDateBeforeOrderDate()
    {
        var act = () => OrderFactory.Create(
            orderDate: new DateOnly(2026, 9, 10),
            deliveryDate: new DateOnly(2026, 9, 9));

        act.Should().Throw<DomainException>()
            .WithMessage("*Teslim tarihi*");
    }

    [Fact]
    public void Create_ShouldReject_EmptyCustomer()
    {
        var act = () => OrderFactory.Create(customerName: "  ");

        act.Should().Throw<DomainException>()
            .WithMessage("*Sipariş veren kişi*");
    }

    [Fact]
    public void Create_ShouldReject_InvalidDeliveryPlace()
    {
        var act = () => Order.Create(
            "LK-20260910-0001",
            "Ahmet",
            new DateOnly(2026, 9, 10),
            new DateOnly(2026, 9, 12),
            (DeliveryPlace)99,
            [OrderItem.Create(1, 80, 120, 5, 1, PanelSide.TekYon)],
            "user-1");

        act.Should().Throw<DomainException>().WithMessage("*Teslim yeri*");
    }

    [Fact]
    public void Create_ShouldReject_NoItems()
    {
        var act = () => Order.Create(
            "LK-20260910-0001",
            "Ahmet",
            new DateOnly(2026, 9, 10),
            new DateOnly(2026, 9, 12),
            DeliveryPlace.Sirket,
            [],
            "user-1");

        act.Should().Throw<DomainException>()
            .WithMessage("*en az bir kalem*");
    }

    [Theory]
    [InlineData(0, 120, 5, 1, "Yatay")]
    [InlineData(80, 0, 5, 1, "Dikey")]
    [InlineData(80, 120, 0, 1, "Derinlik")]
    [InlineData(80, 120, 5, 0, "Adet")]
    public void CreateItem_ShouldReject_NonPositiveValues(int width, int height, int depth, int qty, string expected)
    {
        var act = () => OrderItem.Create(1, width, height, depth, qty, PanelSide.TekYon);

        act.Should().Throw<DomainException>()
            .WithMessage($"*{expected}*");
    }

    [Fact]
    public void CreateItem_ShouldReject_InvalidSide()
    {
        var act = () => OrderItem.Create(1, 80, 120, 5, 1, (PanelSide)99);

        act.Should().Throw<DomainException>().WithMessage("*Yön*");
    }

    [Fact]
    public void CreateItem_ShouldDefaultToFiveCmDepthAndTekYon_WhenFactoryUsesThose()
    {
        var item = OrderItem.Create(1, 80, 120, 5, 2, PanelSide.TekYon);

        item.DepthCm.Should().Be(5);
        item.Side.Should().Be(PanelSide.TekYon);
    }

    [Fact]
    public void Create_ShouldStartAsYeni()
    {
        var order = OrderFactory.Create();

        order.Status.Should().Be(OrderStatus.Yeni);
        order.Items.Should().HaveCount(1);
        order.Items.First().WidthCm.Should().Be(80);
        order.Items.First().HeightCm.Should().Be(120);
        order.Items.First().DepthCm.Should().Be(5);
        order.Items.First().Side.Should().Be(PanelSide.TekYon);
    }

    [Fact]
    public void Transition_ShouldFollowHappyPath()
    {
        var order = OrderFactory.Create();

        order.TransitionTo(OrderStatus.Onaylandi, "u1");
        order.TransitionTo(OrderStatus.Uretimde, "u1");
        order.TransitionTo(OrderStatus.Hazir, "u1");
        order.TransitionTo(OrderStatus.TeslimEdildi, "u1");

        order.Status.Should().Be(OrderStatus.TeslimEdildi);
    }

    [Fact]
    public void Transition_ShouldAllowCancelFromYeni()
    {
        var order = OrderFactory.Create();
        order.TransitionTo(OrderStatus.Iptal, "u1");
        order.Status.Should().Be(OrderStatus.Iptal);
    }

    [Fact]
    public void Transition_ShouldRejectSkip()
    {
        var order = OrderFactory.Create();
        var act = () => order.TransitionTo(OrderStatus.Hazir, "u1");
        act.Should().Throw<DomainException>().WithMessage("*Geçersiz durum geçişi*");
    }

    [Fact]
    public void Update_ShouldBeBlocked_AfterProduction()
    {
        var order = OrderFactory.Create();
        order.TransitionTo(OrderStatus.Onaylandi, "u1");
        order.TransitionTo(OrderStatus.Uretimde, "u1");

        var act = () => order.UpdateHeader(
            "Mehmet",
            order.OrderDate,
            order.DeliveryDate,
            DeliveryPlace.Fabrika,
            null,
            "u1");

        act.Should().Throw<DomainException>().WithMessage("*düzenlenemez*");
    }

    [Fact]
    public void UpdateHeader_ShouldChangeDeliveryPlace()
    {
        var order = OrderFactory.Create();
        order.UpdateHeader(
            order.CustomerName,
            order.OrderDate,
            order.DeliveryDate,
            DeliveryPlace.Fabrika,
            null,
            "u1");

        order.DeliveryPlace.Should().Be(DeliveryPlace.Fabrika);
    }

    [Fact]
    public void OrderNumber_ShouldUseDailySequence()
    {
        OrderNumberFormatter.Format(new DateOnly(2026, 9, 10), 7)
            .Should().Be("LK-20260910-0007");
    }
}

internal static class OrderFactory
{
    public static Order Create(
        string customerName = "Ahmet Yılmaz",
        DateOnly? orderDate = null,
        DateOnly? deliveryDate = null)
    {
        var order = orderDate ?? new DateOnly(2026, 9, 10);
        var delivery = deliveryDate ?? new DateOnly(2026, 9, 12);
        return Order.Create(
            OrderNumberFormatter.Format(order, 1),
            customerName,
            order,
            delivery,
            DeliveryPlace.Sirket,
            [OrderItem.Create(1, 80, 120, 5, 2, PanelSide.TekYon, "köşe kesim")],
            "user-1");
    }
}
