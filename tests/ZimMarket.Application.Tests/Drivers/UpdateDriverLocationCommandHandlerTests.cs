using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ZimMarket.Application.Common;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Application.Drivers;
using ZimMarket.Domain.Entities.Logistics;
using ZimMarket.Domain.Entities.Orders;
using ZimMarket.Domain.Entities.Users;
using ZimMarket.Domain.Enums;
using ZimMarket.Domain.Interfaces;
using ZimMarket.Domain.Interfaces.Repositories;
using ZimMarket.Domain.ValueObjects;

namespace ZimMarket.Application.Tests.Drivers;

public sealed class UpdateDriverLocationCommandHandlerTests
{
    private static readonly PhoneNumber TestPhone = PhoneNumber.Create("+263771234567").Value!;

    [Fact]
    public async Task UpdateDriverLocation_non_driver_returns_DRIVER_LOCATION_FORBIDDEN()
    {
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var cache = Substitute.For<ICacheService>();
        var handler = new UpdateDriverLocationCommandHandler(
            unitOfWork,
            CreateCustomerCurrentUser(),
            cache,
            NullLogger<UpdateDriverLocationCommandHandler>.Instance);

        var result = await handler.Handle(new UpdateDriverLocationCommand(-17.8, 31.05), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(DriverLocationErrorCodes.DriverLocationForbidden);
        await unitOfWork.Drivers.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateDriverLocation_offline_returns_success_without_side_effects()
    {
        Guid driverId = Guid.NewGuid();
        var driver = CreateDriver(driverId, DriverStatus.Offline);

        var unitOfWork = Substitute.For<IUnitOfWork>();
        var drivers = Substitute.For<IUserRepository<Driver>>();
        var cache = Substitute.For<ICacheService>();
        unitOfWork.Drivers.Returns(drivers);
        drivers.GetByIdAsync(driverId, Arg.Any<CancellationToken>()).Returns(driver);

        var handler = new UpdateDriverLocationCommandHandler(
            unitOfWork,
            CreateDriverCurrentUser(driverId),
            cache,
            NullLogger<UpdateDriverLocationCommandHandler>.Instance);

        var result = await handler.Handle(new UpdateDriverLocationCommand(-17.8, 31.05), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await unitOfWork.DriverLocations.DidNotReceive()
            .UpsertPositionAsync(Arg.Any<Guid>(), Arg.Any<double>(), Arg.Any<double>(), Arg.Any<CancellationToken>());
        await cache.DidNotReceive()
            .SetAsync(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<TimeSpan?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateDriverLocation_on_delivery_upserts_cache_and_updates_driver()
    {
        Guid driverId = Guid.NewGuid();
        Guid orderId = Guid.NewGuid();
        var driver = CreateDriver(driverId, DriverStatus.OnDelivery);

        var unitOfWork = Substitute.For<IUnitOfWork>();
        var drivers = Substitute.For<IUserRepository<Driver>>();
        var driverLocations = Substitute.For<IDriverLocationRepository>();
        var deliveryBatches = Substitute.For<IDeliveryBatchRepository>();
        var orders = Substitute.For<IOrderRepository>();
        var cache = Substitute.For<ICacheService>();

        unitOfWork.Drivers.Returns(drivers);
        unitOfWork.DriverLocations.Returns(driverLocations);
        unitOfWork.DeliveryBatches.Returns(deliveryBatches);
        unitOfWork.Orders.Returns(orders);

        drivers.GetByIdAsync(driverId, Arg.Any<CancellationToken>()).Returns(driver);

        var batch = DeliveryBatch.Create(
            Guid.NewGuid(),
            driverId,
            Guid.NewGuid(),
            [orderId],
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow).Value!;
        deliveryBatches.GetActiveByDriverAsync(driverId, Arg.Any<CancellationToken>()).Returns(batch);

        orders.GetByIdAsync(orderId, Arg.Any<CancellationToken>())
            .Returns((Order?)null);

        var handler = new UpdateDriverLocationCommandHandler(
            unitOfWork,
            CreateDriverCurrentUser(driverId),
            cache,
            NullLogger<UpdateDriverLocationCommandHandler>.Instance);

        var result = await handler.Handle(new UpdateDriverLocationCommand(-17.8259, 31.0534), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await driverLocations.Received(1).UpsertPositionAsync(
            driverId,
            -17.8259,
            31.0534,
            Arg.Any<CancellationToken>());
        await cache.Received(1).SetAsync(
            DriverLocationCache.Key(driverId),
            Arg.Is<DriverLocationCachePayload>(p =>
                p.Latitude == -17.8259 && p.Longitude == 31.0534),
            DriverLocationCache.Ttl,
            Arg.Any<CancellationToken>());
        await drivers.Received(1).UpdateAsync(driver, Arg.Any<CancellationToken>());
        driver.LastKnownLocation.Should().NotBeNull();
        driver.LastKnownLocation!.Latitude.Should().Be(-17.8259);
    }

    private static Driver CreateDriver(Guid id, DriverStatus status)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        return new Driver(
            id,
            "d@test.local",
            "D",
            TestPhone,
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
