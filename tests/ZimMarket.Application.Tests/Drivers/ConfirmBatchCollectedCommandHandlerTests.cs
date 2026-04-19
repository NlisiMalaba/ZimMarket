using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ZimMarket.Application.Common;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Application.Drivers;
using ZimMarket.Application.Logistics;
using ZimMarket.Domain.Entities.Logistics;
using ZimMarket.Domain.Entities.Orders;
using ZimMarket.Domain.Enums;
using ZimMarket.Domain.Interfaces;
using ZimMarket.Domain.Interfaces.Repositories;
using ZimMarket.Domain.ValueObjects;

namespace ZimMarket.Application.Tests.Drivers;

public sealed class ConfirmBatchCollectedCommandHandlerTests
{
    private static readonly Address TestDeliveryAddress =
        Address.Create("2 Delivery Rd", "Suburb", "Bulawayo", "Zimbabwe").Value!;

    [Fact]
    public async Task ConfirmBatchCollected_non_driver_returns_DRIVER_FORBIDDEN()
    {
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var handler = new ConfirmBatchCollectedCommandHandler(
            unitOfWork,
            CreateCustomerCurrentUser(),
            NullLogger<ConfirmBatchCollectedCommandHandler>.Instance);

        var result = await handler.Handle(new ConfirmBatchCollectedCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(DriverDeliveryErrorCodes.DriverForbidden);
        await unitOfWork.DeliveryBatches.DidNotReceive()
            .GetByIdForUpdateAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ConfirmBatchCollected_wrong_driver_returns_BATCH_FORBIDDEN()
    {
        Guid batchId = Guid.NewGuid();
        Guid assignedDriver = Guid.NewGuid();
        Guid callerDriver = Guid.NewGuid();

        DeliveryBatch batch = DeliveryBatch.Create(
            batchId,
            assignedDriver,
            Guid.NewGuid(),
            [Guid.NewGuid()],
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow).Value!;

        var unitOfWork = Substitute.For<IUnitOfWork>();
        var batches = Substitute.For<IDeliveryBatchRepository>();
        unitOfWork.DeliveryBatches.Returns(batches);
        batches.GetByIdForUpdateAsync(batchId, Arg.Any<CancellationToken>()).Returns(batch);

        var handler = new ConfirmBatchCollectedCommandHandler(
            unitOfWork,
            CreateDriverCurrentUser(callerDriver),
            NullLogger<ConfirmBatchCollectedCommandHandler>.Instance);

        var result = await handler.Handle(new ConfirmBatchCollectedCommand(batchId), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(LogisticsErrorCodes.DeliveryBatchForbidden);
    }

    [Fact]
    public async Task ConfirmBatchCollected_happy_path_marks_batch_and_orders()
    {
        Guid batchId = Guid.NewGuid();
        Guid driverId = Guid.NewGuid();
        Guid orderId = Guid.NewGuid();
        Guid customerId = Guid.NewGuid();

        Order order = CreateBatchedOrder(orderId, customerId);
        DeliveryBatch batch = DeliveryBatch.Create(
            batchId,
            driverId,
            Guid.NewGuid(),
            [orderId],
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow).Value!;

        var unitOfWork = Substitute.For<IUnitOfWork>();
        var batches = Substitute.For<IDeliveryBatchRepository>();
        var orders = Substitute.For<IOrderRepository>();
        unitOfWork.DeliveryBatches.Returns(batches);
        unitOfWork.Orders.Returns(orders);

        batches.GetByIdForUpdateAsync(batchId, Arg.Any<CancellationToken>()).Returns(batch);
        orders.GetByIdForUpdateAsync(orderId, Arg.Any<CancellationToken>()).Returns(order);

        var handler = new ConfirmBatchCollectedCommandHandler(
            unitOfWork,
            CreateDriverCurrentUser(driverId),
            NullLogger<ConfirmBatchCollectedCommandHandler>.Instance);

        var result = await handler.Handle(new ConfirmBatchCollectedCommand(batchId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        batch.Status.Should().Be(DeliveryBatchStatus.Collected);
        batch.CollectedAt.Should().NotBeNull();
        order.Status.Should().Be(OrderStatus.OutForDelivery);
        await batches.Received(1).UpdateAsync(batch, Arg.Any<CancellationToken>());
        await orders.Received(1).UpdateAsync(order, Arg.Any<CancellationToken>());
    }

    private static Order CreateBatchedOrder(Guid orderId, Guid customerId)
    {
        var price = Money.Create(10m, Currency.USD).Value!;
        var orderItem = OrderItem.Create(Guid.NewGuid(), "Line", price, quantity: 1).Value!;
        var order = Order.Create(
            orderId,
            customerId,
            [orderItem],
            TestDeliveryAddress,
            Money.Create(10m, Currency.USD).Value!,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow).Value!;
        order.ConfirmPayment("ref");
        order.MarkArrivedAtWarehouse(Guid.NewGuid());
        order.PopDomainEvents();
        order.UpdateStatus(OrderStatus.QcPassed);
        order.UpdateStatus(OrderStatus.Batched);
        return order;
    }

    private static ICurrentUser CreateCustomerCurrentUser()
    {
        var u = Substitute.For<ICurrentUser>();
        u.IsAuthenticated.Returns(true);
        u.UserId.Returns(Guid.NewGuid());
        u.Role.Returns(UserRole.Customer);
        return u;
    }

    private static ICurrentUser CreateDriverCurrentUser(Guid driverId)
    {
        var u = Substitute.For<ICurrentUser>();
        u.IsAuthenticated.Returns(true);
        u.UserId.Returns(driverId);
        u.Role.Returns(UserRole.Driver);
        return u;
    }
}
