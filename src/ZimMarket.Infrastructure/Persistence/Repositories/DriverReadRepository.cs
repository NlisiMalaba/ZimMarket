using Microsoft.EntityFrameworkCore;
using ZimMarket.Domain.Entities.Users;
using ZimMarket.Domain.Enums;
using ZimMarket.Domain.Interfaces.Repositories;

namespace ZimMarket.Infrastructure.Persistence.Repositories;

internal sealed class DriverReadRepository : IDriverReadRepository
{
    private readonly AppDbContext _dbContext;

    public DriverReadRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<IReadOnlyList<Guid>> GetDriverIdsByStatusAsync(
        DriverStatus driverStatus,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Drivers
            .AsNoTracking()
            .Where(d => d.DriverStatus == driverStatus)
            .Select(d => d.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
