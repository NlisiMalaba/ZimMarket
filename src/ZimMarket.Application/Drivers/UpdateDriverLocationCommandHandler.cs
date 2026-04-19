using MediatR;
using Microsoft.Extensions.Logging;
using ZimMarket.Application.Common;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Application.Common.Models;
using ZimMarket.Domain.Entities.Logistics;
using ZimMarket.Domain.Entities.Orders;
using ZimMarket.Domain.Entities.Users;
using ZimMarket.Domain.Enums;
using ZimMarket.Domain.Interfaces;
using ZimMarket.Domain.ValueObjects;

namespace ZimMarket.Application.Drivers;

public sealed class UpdateDriverLocationCommandHandler : IRequestHandler<UpdateDriverLocationCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly ICacheService _cache;
    private readonly ILogger<UpdateDriverLocationCommandHandler> _logger;

    public UpdateDriverLocationCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        ICacheService cache,
        ILogger<UpdateDriverLocationCommandHandler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result> Handle(UpdateDriverLocationCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId == Guid.Empty || _currentUser.Role != UserRole.Driver)
        {
            _logger.LogDebug("Update driver location rejected: caller is not an authenticated driver.");
            return Result.Failure(
                DriverLocationErrorCodes.DriverLocationForbidden,
                "Only authenticated drivers can update location.");
        }

        Guid driverId = _currentUser.UserId;

        Driver? driver = await _unitOfWork.Drivers
            .GetByIdAsync(driverId, cancellationToken)
            .ConfigureAwait(false);

        if (driver is null)
        {
            return Result.Failure(
                DriverLocationErrorCodes.DriverLocationForbidden,
                "Driver profile was not found.");
        }

        if (driver.DriverStatus == DriverStatus.Offline)
        {
            _logger.LogDebug("Driver {DriverId} is offline; location update ignored.", driverId);
            return Result.Success();
        }

        if (driver.DriverStatus != DriverStatus.OnDelivery)
        {
            return Result.Failure(
                DriverLocationErrorCodes.DriverNotOnDelivery,
                "Location updates are only accepted while the driver is on delivery.");
        }

        ZimMarket.Shared.Result<GeoCoordinate> coordinateResult = GeoCoordinate.Create(request.Latitude, request.Longitude);
        if (coordinateResult.IsFailure)
        {
            return Result.Failure(
                DriverLocationErrorCodes.DriverLocationInvalidCoordinates,
                string.Join("; ", coordinateResult.Errors));
        }

        GeoCoordinate geo = coordinateResult.Value!;

        DeliveryBatch? activeBatch = await _unitOfWork.DeliveryBatches
            .GetActiveByDriverAsync(driverId, cancellationToken)
            .ConfigureAwait(false);

        List<Guid> activeOrderIds = [];
        if (activeBatch is not null)
        {
            foreach (Guid orderId in activeBatch.OrderIds)
            {
                Order? order = await _unitOfWork.Orders
                    .GetByIdAsync(orderId, cancellationToken)
                    .ConfigureAwait(false);

                if (order is null)
                    continue;

                if (order.Status is OrderStatus.Batched or OrderStatus.OutForDelivery)
                    activeOrderIds.Add(orderId);
            }
        }

        await _unitOfWork.DriverLocations
            .UpsertPositionAsync(driverId, geo.Latitude, geo.Longitude, cancellationToken)
            .ConfigureAwait(false);

        DateTimeOffset updatedAt = DateTimeOffset.UtcNow;
        await _cache
            .SetAsync(
                DriverLocationCache.Key(driverId),
                new DriverLocationCachePayload(geo.Latitude, geo.Longitude, updatedAt),
                DriverLocationCache.Ttl,
                cancellationToken)
            .ConfigureAwait(false);

        driver.UpdateLocation(geo, activeOrderIds);

        await _unitOfWork.Drivers.UpdateAsync(driver, cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
