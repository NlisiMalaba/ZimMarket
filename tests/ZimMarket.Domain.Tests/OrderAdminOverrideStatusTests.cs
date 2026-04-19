using FluentAssertions;
using ZimMarket.Domain.Entities.Orders;
using ZimMarket.Domain.Enums;
using ZimMarket.Domain.Events;
using ZimMarket.Domain.Exceptions;
using ZimMarket.Domain.ValueObjects;

namespace ZimMarket.Domain.Tests;

public sealed class OrderAdminOverrideStatusTests
{
    private static readonly Address Delivery =
        Address.Create("2 Delivery Rd", "Suburb", "Bulawayo", "Zimbabwe").Value!;

    [Fact]
    public void OverrideStatusByAdmin_skips_CanTransitionTo()
    {
        Order order = CreatePendingPaidOrder();
        order.Status.Should().Be(OrderStatus.Paid);
        order.PopDomainEvents();

        order.OverrideStatusByAdmin(OrderStatus.Delivered, "Customer collected in person — manual close-out.");

        order.Status.Should().Be(OrderStatus.Delivered);
        var evt = order.PopDomainEvents().OfType<OrderStatusAdminOverriddenEvent>().Single();
        evt.PreviousStatus.Should().Be(OrderStatus.Paid);
        evt.NewStatus.Should().Be(OrderStatus.Delivered);
        evt.Reason.Should().Contain("manual close-out");
    }

    [Fact]
    public void OverrideStatusByAdmin_same_status_is_noop_and_no_event()
    {
        Order order = CreatePendingPaidOrder();
        order.PopDomainEvents();

        order.OverrideStatusByAdmin(OrderStatus.Paid, "duplicate correction");
        order.Status.Should().Be(OrderStatus.Paid);
        order.PopDomainEvents().Should().BeEmpty();
    }

    [Fact]
    public void OverrideStatusByAdmin_empty_reason_throws()
    {
        Order order = CreatePendingPaidOrder();
        var act = () => order.OverrideStatusByAdmin(OrderStatus.Cancelled, "  ");
        act.Should().Throw<DomainException>().WithMessage("*reason*");
    }

    private static Order CreatePendingPaidOrder()
    {
        Guid productId = Guid.NewGuid();
        var unitPrice = Money.Create(5m, Currency.USD).Value!;
        var orderItem = OrderItem.Create(productId, "Widget", unitPrice, quantity: 1).Value!;
        var order = Order.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [orderItem],
            Delivery,
            Money.Create(5m, Currency.USD).Value!,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow).Value!;

        order.ConfirmPayment("ref-1");
        return order;
    }
}
