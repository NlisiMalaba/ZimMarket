using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ZimMarket.Application.Common;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Application.Orders;
using ZimMarket.Domain.Entities.Orders;
using ZimMarket.Domain.Enums;
using ZimMarket.Domain.Interfaces;
using ZimMarket.Domain.Interfaces.Repositories;
using ZimMarket.Domain.ValueObjects;

namespace ZimMarket.Application.Tests.Orders;

public sealed class OverrideOrderStatusCommandHandlerTests
{
    private static readonly Address Delivery =
        Address.Create("2 Delivery Rd", "Suburb", "Bulawayo", "Zimbabwe").Value!;

    [Fact]
    public async Task Non_admin_returns_forbidden()
    {
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(Guid.NewGuid());
        currentUser.Role.Returns(UserRole.Customer);

        var handler = new OverrideOrderStatusCommandHandler(
            unitOfWork,
            currentUser,
            NullLogger<OverrideOrderStatusCommandHandler>.Instance);

        var result = await handler.Handle(
            new OverrideOrderStatusCommand(Guid.NewGuid(), OrderStatus.Delivered, "Support ticket #1"),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(AdminOrderErrorCodes.Forbidden);
    }

    [Fact]
    public async Task Admin_override_updates_order()
    {
        Order order = CreatePaidOrder();
        var orders = Substitute.For<IOrderRepository>();
        orders.GetByIdForUpdateAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.Orders.Returns(orders);

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(Guid.NewGuid());
        currentUser.Role.Returns(UserRole.Admin);

        var handler = new OverrideOrderStatusCommandHandler(
            unitOfWork,
            currentUser,
            NullLogger<OverrideOrderStatusCommandHandler>.Instance);

        var result = await handler.Handle(
            new OverrideOrderStatusCommand(order.Id, OrderStatus.Delivered, "Manual fulfillment"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Delivered);
        await orders.Received(1).UpdateAsync(order, Arg.Any<CancellationToken>());
    }

    private static Order CreatePaidOrder()
    {
        Guid productId = Guid.NewGuid();
        var unitPrice = Money.Create(3m, Currency.USD).Value!;
        var orderItem = OrderItem.Create(productId, "Item", unitPrice, quantity: 1).Value!;
        var order = Order.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [orderItem],
            Delivery,
            Money.Create(3m, Currency.USD).Value!,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow).Value!;

        order.ConfirmPayment("pay-xyz");
        return order;
    }
}
