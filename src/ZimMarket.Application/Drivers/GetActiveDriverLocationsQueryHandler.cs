using MediatR;
using Microsoft.Extensions.Logging;
using ZimMarket.Application.Common;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Application.Common.Models;
using ZimMarket.Domain.Entities.Logistics;
using ZimMarket.Domain.Enums;
using ZimMarket.Domain.Interfaces;

namespace ZimMarket.Application.Drivers;

public sealed class GetActiveDriverLocationsQueryHandler
    : IRequestHandler<GetActiveDriverLocationsQuery, Result<IReadOnlyList<ActiveDriverLocationDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly ICacheService _cache;
    private readonly ILogger<GetActiveDriverLocationsQueryHandler> _logger;

    public GetActiveDriverLocationsQueryHandler(
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        ICacheService cache,
        ILogger<GetActiveDriverLocationsQueryHandler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<IReadOnlyList<ActiveDriverLocationDto>>> Handle(
        GetActiveDriverLocationsQuery request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated
            || _currentUser.UserId == Guid.Empty
            || (_currentUser.Role != UserRole.Admin && _currentUser.Role != UserRole.SuperAdmin))
        {
            _logger.LogDebug("Get active driver locations rejected: caller is not an admin.");
            return Result<IReadOnlyList<ActiveDriverLocationDto>>.Failure(
                WarehouseErrorCodes.WarehouseForbidden,
                "Only administrators can view active driver locations.");
        }

        IReadOnlyList<Guid> driverIds = await _unitOfWork.DriverRead
            .GetDriverIdsByStatusAsync(DriverStatus.OnDelivery, cancellationToken)
            .ConfigureAwait(false);

        if (driverIds.Count == 0)
        {
            return Result<IReadOnlyList<ActiveDriverLocationDto>>.Success(
                Array.Empty<ActiveDriverLocationDto>());
        }

        IReadOnlyList<Guid> orderedIds = driverIds.OrderBy(x => x).ToList();

        DriverLocationCachePayload?[] redisPayloads = await Task.WhenAll(
            orderedIds.Select(id =>
                _cache.GetAsync<DriverLocationCachePayload>(DriverLocationCache.Key(id), cancellationToken)))
            .ConfigureAwait(false);

        var missingForDb = new List<Guid>();
        var fromRedis = new Dictionary<Guid, ActiveDriverLocationDto>();
        for (int i = 0; i < orderedIds.Count; i++)
        {
            Guid id = orderedIds[i];
            DriverLocationCachePayload? payload = redisPayloads[i];
            if (payload is not null)
            {
                fromRedis[id] = new ActiveDriverLocationDto(
                    id,
                    payload.Latitude,
                    payload.Longitude,
                    payload.UpdatedAtUtc);
            }
            else
            {
                missingForDb.Add(id);
            }
        }

        IReadOnlyDictionary<Guid, DriverLocation> fromDb =
            missingForDb.Count == 0
                ? new Dictionary<Guid, DriverLocation>()
                : await _unitOfWork.DriverLocations
                    .GetPositionsByDriverIdsAsync(missingForDb, cancellationToken)
                    .ConfigureAwait(false);

        var result = new List<ActiveDriverLocationDto>(orderedIds.Count);
        foreach (Guid id in orderedIds)
        {
            if (fromRedis.TryGetValue(id, out ActiveDriverLocationDto? redisDto))
            {
                result.Add(redisDto);
                continue;
            }

            if (fromDb.TryGetValue(id, out DriverLocation? row))
            {
                result.Add(new ActiveDriverLocationDto(id, row.Latitude, row.Longitude, row.UpdatedAt));
                continue;
            }

            result.Add(new ActiveDriverLocationDto(id, null, null, null));
        }

        return Result<IReadOnlyList<ActiveDriverLocationDto>>.Success(result);
    }
}
