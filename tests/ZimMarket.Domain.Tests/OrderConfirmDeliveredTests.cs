using FluentAssertions;
using ZimMarket.Domain.Entities.Orders;
using ZimMarket.Domain.Enums;
using ZimMarket.Domain.Events;
using ZimMarket.Domain.Exceptions;
using ZimMarket.Domain.ValueObjects;

namespace ZimMarket.Domain.Tests;

public sealed class OrderConfirmDeliveredTests
{
    private static readonly Address Address =
        Address.Create("1 St", "Sub", "City", "ZW").Value!;

    [Fact]
    public void ConfirmDelivered_from_OutForDelivery_sets_photo_and_status()
    {
        var price = Money.Create(5m, Currency.USD).Value!;
        var item = OrderItem.Create(Guid.NewGuid(), "P", price, 1).Value!;
        var order = Order.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [item],
            Address,
            Money.Create(5m, Currency.USD).Value!,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow).Value!;
        order.ConfirmPayment("r");
        order.UpdateStatus(OrderStatus.AtWarehouse);
        order.UpdateStatus(OrderStatus.QcPassed);
        order.UpdateStatus(OrderStatus.Batched);
        order.UpdateStatus(OrderStatus.OutForDelivery);
        order.PopDomainEvents();

        const string key = "delivery-photos/u/abc.jpg";
        order.ConfirmDelivered(key);

        order.Status.Should().Be(OrderStatus.Delivered);
        order.DeliveryPhotoKey.Should().Be(key);
        order.PopDomainEvents().Should().ContainSingle(e => e is OrderDeliveredEvent);
    }

    [Fact]
    public void ConfirmDelivered_from_Batched_throws()
    {
        var price = Money.Create(5m, Currency.USD).Value!;
        var item = OrderItem.Create(Guid.NewGuid(), "P", price, 1).Value!;
        var order = Order.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [item],
            Address,
            Money.Create(5m, Currency.USD).Value!,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow).Value!;
        order.ConfirmPayment("r");
        order.UpdateStatus(OrderStatus.AtWarehouse);
        order.UpdateStatus(OrderStatus.QcPassed);
        order.UpdateStatus(OrderStatus.Batched);

        Action act = () => order.ConfirmDelivered("delivery-photos/x/y.jpg");

        act.Should().Throw<DomainException>();
    }
}
