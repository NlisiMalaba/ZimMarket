using ZimMarket.Domain.Enums;

namespace ZimMarket.Domain.Interfaces.Repositories;

public interface IDriverReadRepository
{
    Task<IReadOnlyList<Guid>> GetDriverIdsByStatusAsync(
        DriverStatus driverStatus,
        CancellationToken cancellationToken = default);
}
