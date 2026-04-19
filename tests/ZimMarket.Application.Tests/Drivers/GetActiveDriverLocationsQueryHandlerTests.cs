using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ZimMarket.Application.Common;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Application.Drivers;
using ZimMarket.Domain.Entities.Logistics;
using ZimMarket.Domain.Enums;
using ZimMarket.Domain.Interfaces;
using ZimMarket.Domain.Interfaces.Repositories;

namespace ZimMarket.Application.Tests.Drivers;

public sealed class GetActiveDriverLocationsQueryHandlerTests
{
    [Fact]
    public async Task Non_admin_returns_WAREHOUSE_FORBIDDEN()
    {
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var cache = Substitute.For<ICacheService>();
        var handler = new GetActiveDriverLocationsQueryHandler(
            unitOfWork,
            CreateCustomerCurrentUser(),
            cache,
            NullLogger<GetActiveDriverLocationsQueryHandler>.Instance);

        var result = await handler.Handle(new GetActiveDriverLocationsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(WarehouseErrorCodes.WarehouseForbidden);
        await unitOfWork.DriverRead.DidNotReceive()
            .GetDriverIdsByStatusAsync(Arg.Any<DriverStatus>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task No_on_delivery_drivers_returns_empty_list()
    {
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var driverRead = Substitute.For<IDriverReadRepository>();
        unitOfWork.DriverRead.Returns(driverRead);
        driverRead.GetDriverIdsByStatusAsync(DriverStatus.OnDelivery, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Guid>());

        var cache = Substitute.For<ICacheService>();
        var handler = new GetActiveDriverLocationsQueryHandler(
            unitOfWork,
            CreateAdminCurrentUser(),
            cache,
            NullLogger<GetActiveDriverLocationsQueryHandler>.Instance);

        var result = await handler.Handle(new GetActiveDriverLocationsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
        await cache.DidNotReceive()
            .GetAsync<DriverLocationCachePayload>(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Redis_hit_uses_cache_payload()
    {
        Guid driverId = Guid.NewGuid();
        var updated = DateTimeOffset.UtcNow;
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var driverRead = Substitute.For<IDriverReadRepository>();
        var driverLocations = Substitute.For<IDriverLocationRepository>();
        unitOfWork.DriverRead.Returns(driverRead);
        unitOfWork.DriverLocations.Returns(driverLocations);
        driverRead.GetDriverIdsByStatusAsync(DriverStatus.OnDelivery, Arg.Any<CancellationToken>())
            .Returns(new[] { driverId });

        var cache = Substitute.For<ICacheService>();
        cache.GetAsync<DriverLocationCachePayload>(DriverLocationCache.Key(driverId), Arg.Any<CancellationToken>())
            .Returns(new DriverLocationCachePayload(-17.8, 31.05, updated));

        var handler = new GetActiveDriverLocationsQueryHandler(
            unitOfWork,
            CreateAdminCurrentUser(),
            cache,
            NullLogger<GetActiveDriverLocationsQueryHandler>.Instance);

        var result = await handler.Handle(new GetActiveDriverLocationsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var dto = result.Value!.Single();
        dto.DriverId.Should().Be(driverId);
        dto.Latitude.Should().Be(-17.8);
        dto.Longitude.Should().Be(31.05);
        dto.UpdatedAtUtc.Should().Be(updated);
        await driverLocations.DidNotReceive()
            .GetPositionsByDriverIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Redis_miss_falls_back_to_database()
    {
        Guid driverId = Guid.NewGuid();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var driverRead = Substitute.For<IDriverReadRepository>();
        var driverLocations = Substitute.For<IDriverLocationRepository>();
        unitOfWork.DriverRead.Returns(driverRead);
        unitOfWork.DriverLocations.Returns(driverLocations);
        driverRead.GetDriverIdsByStatusAsync(DriverStatus.OnDelivery, Arg.Any<CancellationToken>())
            .Returns(new[] { driverId });

        var cache = Substitute.For<ICacheService>();
        cache.GetAsync<DriverLocationCachePayload>(DriverLocationCache.Key(driverId), Arg.Any<CancellationToken>())
            .Returns((DriverLocationCachePayload?)null);

        var rowUpdated = DateTimeOffset.UtcNow.AddMinutes(-2);
        var row = DriverLocation.Create(driverId, -18.0, 31.1, rowUpdated, rowUpdated);
        driverLocations.GetPositionsByDriverIdsAsync(
                Arg.Is<IReadOnlyCollection<Guid>>(ids => ids.Count == 1 && ids.Contains(driverId)),
                Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, DriverLocation> { [driverId] = row });

        var handler = new GetActiveDriverLocationsQueryHandler(
            unitOfWork,
            CreateAdminCurrentUser(),
            cache,
            NullLogger<GetActiveDriverLocationsQueryHandler>.Instance);

        var result = await handler.Handle(new GetActiveDriverLocationsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var dto = result.Value!.Single();
        dto.DriverId.Should().Be(driverId);
        dto.Latitude.Should().Be(-18.0);
        dto.Longitude.Should().Be(31.1);
        dto.UpdatedAtUtc.Should().Be(rowUpdated);
    }

    [Fact]
    public async Task Redis_and_database_miss_returns_null_coordinates()
    {
        Guid driverId = Guid.NewGuid();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var driverRead = Substitute.For<IDriverReadRepository>();
        var driverLocations = Substitute.For<IDriverLocationRepository>();
        unitOfWork.DriverRead.Returns(driverRead);
        unitOfWork.DriverLocations.Returns(driverLocations);
        driverRead.GetDriverIdsByStatusAsync(DriverStatus.OnDelivery, Arg.Any<CancellationToken>())
            .Returns(new[] { driverId });

        var cache = Substitute.For<ICacheService>();
        cache.GetAsync<DriverLocationCachePayload>(DriverLocationCache.Key(driverId), Arg.Any<CancellationToken>())
            .Returns((DriverLocationCachePayload?)null);

        driverLocations.GetPositionsByDriverIdsAsync(
                Arg.Any<IReadOnlyCollection<Guid>>(),
                Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, DriverLocation>());

        var handler = new GetActiveDriverLocationsQueryHandler(
            unitOfWork,
            CreateAdminCurrentUser(),
            cache,
            NullLogger<GetActiveDriverLocationsQueryHandler>.Instance);

        var result = await handler.Handle(new GetActiveDriverLocationsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var dto = result.Value!.Single();
        dto.DriverId.Should().Be(driverId);
        dto.Latitude.Should().BeNull();
        dto.Longitude.Should().BeNull();
        dto.UpdatedAtUtc.Should().BeNull();
    }

    [Fact]
    public async Task SuperAdmin_can_query()
    {
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var driverRead = Substitute.For<IDriverReadRepository>();
        unitOfWork.DriverRead.Returns(driverRead);
        driverRead.GetDriverIdsByStatusAsync(DriverStatus.OnDelivery, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Guid>());

        var cache = Substitute.For<ICacheService>();
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(Guid.NewGuid());
        currentUser.Role.Returns(UserRole.SuperAdmin);

        var handler = new GetActiveDriverLocationsQueryHandler(
            unitOfWork,
            currentUser,
            cache,
            NullLogger<GetActiveDriverLocationsQueryHandler>.Instance);

        var result = await handler.Handle(new GetActiveDriverLocationsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
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
