using ZimMarket.Domain.ReadModels;

namespace ZimMarket.Domain.Interfaces.Repositories;

public interface IDashboardStatsReadRepository
{
    /// <summary>
    /// Loads dashboard aggregates for the UTC calendar day [<paramref name="utcDayStart"/>, <paramref name="utcDayEndExclusive"/>).
    /// </summary>
    Task<DashboardStatsRaw> GetAsync(
        DateTimeOffset utcDayStart,
        DateTimeOffset utcDayEndExclusive,
        int lowStockMaxQuantityInclusive,
        CancellationToken cancellationToken = default);
}
