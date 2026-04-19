using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using ZimMarket.Application.Common;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Application.Logistics;
using ZimMarket.Domain.Entities.Logistics;
using ZimMarket.Domain.Entities.Orders;
using ZimMarket.Domain.Entities.Users;
using ZimMarket.Domain.Entities.Warehouse;
using ZimMarket.Domain.Enums;
using ZimMarket.Domain.Interfaces;
using ZimMarket.Domain.Interfaces.Repositories;
using ZimMarket.Domain.ValueObjects;

namespace ZimMarket.Application.Tests.Logistics;

public sealed class CreateDeliveryBatchCommandHandlerTests
{
    private static readonly PhoneNumber TestPhone = PhoneNumber.Create("+263771234567").Value!;

    private static readonly Address TestDeliveryAddress =
        Address.Create("2 Delivery Rd", "Suburb", "Bulawayo", "Zimbabwe").Value!;

    [Fact]
    public async Task CreateDeliveryBatch_non_admin_returns_LOGISTICS_FORBIDDEN()
    {
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var handler = new CreateDeliveryBatchCommandHandler(
            unitOfWork,
            CreateCustomerCurrentUser(),
            Options.Create(new LogisticsOptions()),
            NullLogger<CreateDeliveryBatchCommandHandler>.Instance);

        var result = await handler.Handle(
            new CreateDeliveryBatchCommand([Guid.NewGuid()], Guid.NewGuid()),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(LogisticsErrorCodes.LogisticsForbidden);
        await unitOfWork.Orders.DidNotReceive()
            .GetByIdForUpdateAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateDeliveryBatch_happy_path_creates_batch_transitions_orders_and_driver()
    {
        Guid orderId = Guid.NewGuid();
        Guid productId = Guid.NewGuid();
        Guid customerId = Guid.NewGuid();
        Guid driverId = Guid.NewGuid();
        Guid warehouseId = Guid.Parse("d0000000-0000-4000-8000-000000000099");

        Order order = CreateSingleLineQcPassedOrder(orderId, productId, customerId);
        WarehouseItem line = CreatePassedWarehouseItem(Guid.NewGuid(), orderId, productId);
        Driver driver = CreateEligibleDriver(driverId);

        var unitOfWork = Substitute.For<IUnitOfWork>();
        var orders = Substitute.For<IOrderRepository>();
        var warehouseItems = Substitute.For<IWarehouseItemRepository>();
        var deliveryBatches = Substitute.For<IDeliveryBatchRepository>();
        var drivers = Substitute.For<IUserRepository<Driver>>();
        unitOfWork.Orders.Returns(orders);
        unitOfWork.WarehouseItems.Returns(warehouseItems);
        unitOfWork.DeliveryBatches.Returns(deliveryBatches);
        unitOfWork.Drivers.Returns(drivers);

        orders.GetByIdForUpdateAsync(orderId, Arg.Any<CancellationToken>()).Returns(order);
        warehouseItems.GetByOrderIdForUpdateAsync(orderId, Arg.Any<CancellationToken>())
            .Returns(new List<WarehouseItem> { line });
        deliveryBatches.GetByOrderIdAsync(orderId, Arg.Any<CancellationToken>()).Returns((DeliveryBatch?)null);
        deliveryBatches.GetActiveByDriverAsync(driverId, Arg.Any<CancellationToken>())
            .Returns((DeliveryBatch?)null);
        drivers.GetByIdAsync(driverId, Arg.Any<CancellationToken>()).Returns(driver);

        DeliveryBatch? addedBatch = null;
        await deliveryBatches.AddAsync(
            Arg.Do<DeliveryBatch>(b => addedBatch = b),
            Arg.Any<CancellationToken>());

        var handler = new CreateDeliveryBatchCommandHandler(
            unitOfWork,
            CreateAdminCurrentUser(),
            Options.Create(new LogisticsOptions { DefaultPickupWarehouseId = warehouseId }),
            NullLogger<CreateDeliveryBatchCommandHandler>.Instance);

        var result = await handler.Handle(
            new CreateDeliveryBatchCommand([orderId], driverId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        order.Status.Should().Be(OrderStatus.Batched);
        driver.DriverStatus.Should().Be(DriverStatus.OnDelivery);
        addedBatch.Should().NotBeNull();
        addedBatch!.DriverId.Should().Be(driverId);
        addedBatch.WarehouseId.Should().Be(warehouseId);
        addedBatch.OrderIds.Should().BeEquivalentTo([orderId]);
        line.BatchId.Should().Be(result.Value);
        await deliveryBatches.Received(1).AddAsync(Arg.Any<DeliveryBatch>(), Arg.Any<CancellationToken>());
        await drivers.Received(1).UpdateAsync(driver, Arg.Any<CancellationToken>());
        await orders.Received(1).UpdateAsync(order, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateDeliveryBatch_driver_offline_returns_DRIVER_NOT_ELIGIBLE()
    {
        Guid orderId = Guid.NewGuid();
        Guid driverId = Guid.NewGuid();
        Order order = CreateSingleLineQcPassedOrder(orderId, Guid.NewGuid(), Guid.NewGuid());
        Driver driver = CreateEligibleDriver(driverId);
        driver.SetStatus(DriverStatus.Offline);

        var unitOfWork = Substitute.For<IUnitOfWork>();
        var orders = Substitute.For<IOrderRepository>();
        var deliveryBatches = Substitute.For<IDeliveryBatchRepository>();
        var drivers = Substitute.For<IUserRepository<Driver>>();
        unitOfWork.Orders.Returns(orders);
        unitOfWork.DeliveryBatches.Returns(deliveryBatches);
        unitOfWork.Drivers.Returns(drivers);

        orders.GetByIdForUpdateAsync(orderId, Arg.Any<CancellationToken>()).Returns(order);
        deliveryBatches.GetByOrderIdAsync(orderId, Arg.Any<CancellationToken>()).Returns((DeliveryBatch?)null);
        drivers.GetByIdAsync(driverId, Arg.Any<CancellationToken>()).Returns(driver);

        var handler = new CreateDeliveryBatchCommandHandler(
            unitOfWork,
            CreateAdminCurrentUser(),
            Options.Create(new LogisticsOptions()),
            NullLogger<CreateDeliveryBatchCommandHandler>.Instance);

        var result = await handler.Handle(
            new CreateDeliveryBatchCommand([orderId], driverId),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(LogisticsErrorCodes.DriverNotEligible);
        await deliveryBatches.DidNotReceive().AddAsync(Arg.Any<DeliveryBatch>(), Arg.Any<CancellationToken>());
    }

    private static Order CreateSingleLineQcPassedOrder(Guid orderId, Guid productId, Guid customerId)
    {
        var price = Money.Create(10m, Currency.USD).Value!;
        var orderItem = OrderItem.Create(productId, "Line", price, quantity: 1).Value!;
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
        return order;
    }

    private static WarehouseItem CreatePassedWarehouseItem(Guid id, Guid orderId, Guid productId)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        WarehouseItem item = WarehouseItem.Create(id, orderId, productId, now, now, now).Value!;
        item.ApplyQcOutcome(WarehouseQcStatus.Passed, false, null);
        return item;
    }

    private static Driver CreateEligibleDriver(Guid id)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        return new Driver(
            id,
            "driver@test.local",
            "Test Driver",
            TestPhone,
            passwordHash: "x",
            KycStatus.Approved,
            isActive: true,
            refreshTokenHash: null,
            refreshTokenExpiry: null,
            createdAt: now,
            updatedAt: now,
            licenseNumber: $"lic-{id:N}",
            licenseDocumentKey: "doc-lic",
            vehicleRegistration: $"veh-{id:N}",
            vehicleDocumentKey: "doc-veh",
            DriverStatus.Available,
            lastKnownLocation: null,
            isApproved: true,
            rejectionReason: null);
    }

    private static ICurrentUser CreateCustomerCurrentUser()
    {
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(Guid.NewGuid());
        currentUser.Role.Returns(UserRole.Customer);
        return currentUser;
    }

    private static ICurrentUser CreateAdminCurrentUser()
    {
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(Guid.NewGuid());
        currentUser.Role.Returns(UserRole.Admin);
        return currentUser;
    }
}
