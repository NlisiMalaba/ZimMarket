using Microsoft.EntityFrameworkCore;
using ZimMarket.Domain.Entities.Logistics;
using ZimMarket.Domain.Interfaces.Repositories;

namespace ZimMarket.Infrastructure.Persistence.Repositories;

internal sealed class DriverLocationRepository : IDriverLocationRepository
{
    private readonly AppDbContext _dbContext;

    public DriverLocationRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task UpsertPositionAsync(
        Guid driverId,
        double latitude,
        double longitude,
        CancellationToken cancellationToken = default)
    {
        DriverLocation? existing = await _dbContext.DriverLocations
            .FirstOrDefaultAsync(x => x.Id == driverId, cancellationToken)
            .ConfigureAwait(false);

        if (existing is null)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            DriverLocation created = DriverLocation.Create(driverId, latitude, longitude, now, now);
            await _dbContext.DriverLocations.AddAsync(created, cancellationToken).ConfigureAwait(false);
            return;
        }

        existing.SetPosition(latitude, longitude);
    }

    public async Task<IReadOnlyDictionary<Guid, DriverLocation>> GetPositionsByDriverIdsAsync(
        IReadOnlyCollection<Guid> driverIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(driverIds);
        if (driverIds.Count == 0)
            return new Dictionary<Guid, DriverLocation>();

        List<DriverLocation> rows = await _dbContext.DriverLocations
            .AsNoTracking()
            .Where(x => driverIds.Contains(x.Id))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return rows.ToDictionary(x => x.Id);
    }
}
