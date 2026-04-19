using ZimMarket.Domain.Entities.Logistics;

namespace ZimMarket.Domain.Interfaces.Repositories;

public interface IDriverLocationRepository
{
    Task UpsertPositionAsync(Guid driverId, double latitude, double longitude, CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<Guid, DriverLocation>> GetPositionsByDriverIdsAsync(
        IReadOnlyCollection<Guid> driverIds,
        CancellationToken cancellationToken = default);
}
