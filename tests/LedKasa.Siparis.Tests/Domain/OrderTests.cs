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
    public void Create_ShouldReject_EmptyDeliveryAddress()
    {
        var blank = () => Order.Create(
            "LK-20260910-0001",
            "Ahmet",
            new DateOnly(2026, 9, 10),
            new DateOnly(2026, 9, 12),
            DeliveryPlace.Sirket,
            "  ",
            [OrderItem.Create(1, 80, 120, 1, 10)],
            "user-1");

        blank.Should().Throw<DomainException>().WithMessage("*teslimat adresi*");
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
            "Organize Sanayi 1. Cadde No:12",
            [],
            "user-1");

        act.Should().Throw<DomainException>()
            .WithMessage("*en az bir kalem*");
    }

    [Theory]
    [InlineData(0, 120, 1, "Yatay")]
    [InlineData(80, 0, 1, "Dikey")]
    [InlineData(80, 120, 0, "Adet")]
    public void CreateItem_ShouldReject_NonPositiveValues(decimal width, decimal height, int qty, string expected)
    {
        var act = () => OrderItem.Create(1, width, height, qty, 1);

        act.Should().Throw<DomainException>()
            .WithMessage($"*{expected}*");
    }

    [Fact]
    public void CreateItem_ShouldReject_NegativePrice()
    {
        var act = () => OrderItem.Create(1, 80, 120, 1, -1);
        act.Should().Throw<DomainException>().WithMessage("*Birim fiyat*");
    }

    [Fact]
    public void Create_ShouldComputeLineAndGrandTotal()
    {
        var order = OrderFactory.Create();

        order.Items.First().UnitPrice.Should().Be(150);
        order.Items.First().LineTotal.Should().Be(300);
        order.GrandTotal.Should().Be(300);
    }

    [Fact]
    public void Create_ShouldStartAsYeni()
    {
        var order = OrderFactory.Create();

        order.Status.Should().Be(OrderStatus.Yeni);
        order.Items.Should().HaveCount(1);
        order.Items.First().WidthCm.Should().Be(80);
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
            "Fabrika deposu",
            Currency.Usd,
            null,
            "u1");

        act.Should().Throw<DomainException>().WithMessage("*düzenlenemez*");
    }

    [Fact]
    public void Create_ShouldDefaultCurrencyToTry()
    {
        OrderFactory.Create().Currency.Should().Be(Currency.Try);
    }

    [Fact]
    public void Create_ShouldAcceptUsd()
    {
        OrderFactory.Create(currency: Currency.Usd).Currency.Should().Be(Currency.Usd);
    }

    [Fact]
    public void Create_ShouldReject_InvalidCurrency()
    {
        var act = () => OrderFactory.Create(currency: (Currency)99);
        act.Should().Throw<DomainException>().WithMessage("*Para birimi*");
    }

    [Fact]
    public void UpdateHeader_ShouldChangeCurrency()
    {
        var order = OrderFactory.Create();
        order.UpdateHeader(
            order.CustomerName,
            order.OrderDate,
            order.DeliveryDate,
            order.DeliveryPlace,
            order.DeliveryAddress,
            Currency.Usd,
            null,
            "u1");

        order.Currency.Should().Be(Currency.Usd);
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
        DateOnly? deliveryDate = null,
        Currency currency = Currency.Try)
    {
        var order = orderDate ?? new DateOnly(2026, 9, 10);
        var delivery = deliveryDate ?? new DateOnly(2026, 9, 12);
        return Order.Create(
            OrderNumberFormatter.Format(order, 1),
            customerName,
            order,
            delivery,
            DeliveryPlace.Sirket,
            "Atatürk Cad. No:10 İstanbul",
            [OrderItem.Create(1, 80, 120, 2, 150m, [1], "köşe kesim")],
            "user-1",
            currency: currency);
    }
}
