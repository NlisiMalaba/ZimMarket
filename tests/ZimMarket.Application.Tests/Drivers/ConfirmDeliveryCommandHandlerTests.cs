using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ZimMarket.Application.Common;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Application.Common.Models;
using ZimMarket.Application.Drivers;
using ZimMarket.Application.Logistics;
using ZimMarket.Domain.Entities.Logistics;
using ZimMarket.Domain.Entities.Orders;
using ZimMarket.Domain.Enums;
using ZimMarket.Domain.Interfaces;
using ZimMarket.Domain.Interfaces.Repositories;
using ZimMarket.Domain.ValueObjects;

namespace ZimMarket.Application.Tests.Drivers;

public sealed class ConfirmDeliveryCommandHandlerTests
{
    private static readonly Address TestDeliveryAddress =
        Address.Create("2 Delivery Rd", "Suburb", "Bulawayo", "Zimbabwe").Value!;

    [Fact]
    public async Task ConfirmDelivery_non_driver_returns_DRIVER_FORBIDDEN()
    {
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var fileStorage = Substitute.For<IFileStorage>();
        var handler = new ConfirmDeliveryCommandHandler(
            unitOfWork,
            CreateCustomerCurrentUser(),
            fileStorage,
            NullLogger<ConfirmDeliveryCommandHandler>.Instance);

        var result = await handler.Handle(
            new ConfirmDeliveryCommand(Guid.NewGuid(), Guid.NewGuid(), "delivery-photos/a/b.jpg"),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(DriverDeliveryErrorCodes.DriverForbidden);
        await fileStorage.DidNotReceive().ExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ConfirmDelivery_missing_blob_returns_validation()
    {
        Guid driverId = Guid.NewGuid();
        Guid batchId = Guid.NewGuid();
        Guid orderId = Guid.NewGuid();
        string key = "delivery-photos/x/y.jpg";

        var batch = DeliveryBatch.Create(
            batchId,
            driverId,
            Guid.NewGuid(),
            [orderId],
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow).Value!;
        batch.MarkCollected();
        batch.MarkInTransit();

        Order order = CreateOutForDeliveryOrder(orderId, Guid.NewGuid());

        var unitOfWork = Substitute.For<IUnitOfWork>();
        var batches = Substitute.For<IDeliveryBatchRepository>();
        var orders = Substitute.For<IOrderRepository>();
        var fileStorage = Substitute.For<IFileStorage>();
        unitOfWork.DeliveryBatches.Returns(batches);
        unitOfWork.Orders.Returns(orders);
        batches.GetByIdForUpdateAsync(batchId, Arg.Any<CancellationToken>()).Returns(batch);
        orders.GetByIdForUpdateAsync(orderId, Arg.Any<CancellationToken>()).Returns(order);
        fileStorage.ExistsAsync(key, Arg.Any<CancellationToken>()).Returns(false);

        var handler = new ConfirmDeliveryCommandHandler(
            unitOfWork,
            CreateDriverCurrentUser(driverId),
            fileStorage,
            NullLogger<ConfirmDeliveryCommandHandler>.Instance);

        var result = await handler.Handle(
            new ConfirmDeliveryCommand(batchId, orderId, key),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(Result.ValidationErrorCode);
    }

    [Fact]
    public async Task ConfirmDelivery_single_order_completes_batch_and_frees_driver()
    {
        Guid driverId = Guid.NewGuid();
        Guid batchId = Guid.NewGuid();
        Guid orderId = Guid.NewGuid();
        string key = "delivery-photos/x/y.jpg";

        var batch = DeliveryBatch.Create(
            batchId,
            driverId,
            Guid.NewGuid(),
            [orderId],
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow).Value!;
        batch.MarkCollected();

        Order order = CreateOutForDeliveryOrder(orderId, Guid.NewGuid());
        var driver = CreateDriver(driverId, DriverStatus.OnDelivery);

        var unitOfWork = Substitute.For<IUnitOfWork>();
        var batches = Substitute.For<IDeliveryBatchRepository>();
        var orders = Substitute.For<IOrderRepository>();
        var drivers = Substitute.For<IUserRepository<Domain.Entities.Users.Driver>>();
        var fileStorage = Substitute.For<IFileStorage>();

        unitOfWork.DeliveryBatches.Returns(batches);
        unitOfWork.Orders.Returns(orders);
        unitOfWork.Drivers.Returns(drivers);

        batches.GetByIdForUpdateAsync(batchId, Arg.Any<CancellationToken>()).Returns(batch);
        orders.GetByIdForUpdateAsync(orderId, Arg.Any<CancellationToken>()).Returns(order);
        drivers.GetByIdAsync(driverId, Arg.Any<CancellationToken>()).Returns(driver);
        fileStorage.ExistsAsync(key, Arg.Any<CancellationToken>()).Returns(true);

        var handler = new ConfirmDeliveryCommandHandler(
            unitOfWork,
            CreateDriverCurrentUser(driverId),
            fileStorage,
            NullLogger<ConfirmDeliveryCommandHandler>.Instance);

        var result = await handler.Handle(
            new ConfirmDeliveryCommand(batchId, orderId, key),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Delivered);
        order.DeliveryPhotoKey.Should().Be(key);
        batch.Status.Should().Be(DeliveryBatchStatus.Completed);
        driver.DriverStatus.Should().Be(DriverStatus.Available);
        await drivers.Received(1).UpdateAsync(driver, Arg.Any<CancellationToken>());
        await batches.Received(1).UpdateAsync(batch, Arg.Any<CancellationToken>());
    }

    private static Order CreateOutForDeliveryOrder(Guid orderId, Guid customerId)
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
        order.UpdateStatus(OrderStatus.AtWarehouse);
        order.UpdateStatus(OrderStatus.QcPassed);
        order.UpdateStatus(OrderStatus.Batched);
        order.UpdateStatus(OrderStatus.OutForDelivery);
        return order;
    }

    private static Domain.Entities.Users.Driver CreateDriver(Guid id, DriverStatus status)
    {
        var phone = PhoneNumber.Create("+263771111111").Value!;
        DateTimeOffset now = DateTimeOffset.UtcNow;
        return new Domain.Entities.Users.Driver(
            id,
            "d@test.local",
            "D",
            phone,
            "hash",
            KycStatus.Approved,
            isActive: true,
            refreshTokenHash: null,
            refreshTokenExpiry: null,
            createdAt: now,
            updatedAt: now,
            licenseNumber: $"lic-{id:N}",
            licenseDocumentKey: "k",
            vehicleRegistration: $"veh-{id:N}",
            vehicleDocumentKey: "k2",
            status,
            lastKnownLocation: null,
            isApproved: true,
            rejectionReason: null);
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
