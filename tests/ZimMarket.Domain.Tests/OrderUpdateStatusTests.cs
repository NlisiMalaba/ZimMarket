using ZimMarket.Domain.Entities.Orders;
using ZimMarket.Domain.Enums;
using ZimMarket.Domain.Exceptions;
using ZimMarket.Domain.ValueObjects;
using FluentAssertions;

namespace ZimMarket.Domain.Tests;

public class OrderUpdateStatusTests
{
    [Fact]
    public void UpdateStatus_from_Pending_to_Delivered_throws()
    {
        var order = CreateOrder();
        var act = () => order.UpdateStatus(OrderStatus.Delivered);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void UpdateStatus_happy_path_sequential_transitions_succeed()
    {
        var order = CreateOrder();
        order.ConfirmPayment("ref-1");

        order.UpdateStatus(OrderStatus.AtWarehouse);
        order.Status.Should().Be(OrderStatus.AtWarehouse);

        order.UpdateStatus(OrderStatus.QcPassed);
        order.UpdateStatus(OrderStatus.Batched);
        order.UpdateStatus(OrderStatus.OutForDelivery);
        order.UpdateStatus(OrderStatus.Delivered);

        order.Status.Should().Be(OrderStatus.Delivered);
    }

    private static Order CreateOrder()
    {
        var item = OrderItem.Create(Guid.NewGuid(), "Widget", DomainTestHelpers.TenUsd, 1).Value!;
        var addr = DomainTestHelpers.ValidAddress;
        return Order.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [item],
            addr,
            DomainTestHelpers.TenUsd,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow).Value!;
    }
}
