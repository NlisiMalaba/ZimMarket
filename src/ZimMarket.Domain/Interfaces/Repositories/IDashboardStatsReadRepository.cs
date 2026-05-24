using ZimMarket.Domain.ReadModels;

namespace ZimMarket.Domain.Interfaces.Repositories;

public interface IDashboardStatsReadRepository
{
    /// <summary>
    /// Loads operational dashboard aggregates for the UTC calendar day
    /// [<paramref name="utcDayStart"/>, <paramref name="utcDayEndExclusive"/>).
    /// </summary>
    Task<DashboardStatsRaw> GetOperationalAsync(
        DateTimeOffset utcDayStart,
        DateTimeOffset utcDayEndExclusive,
        int lowStockMaxQuantityInclusive,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads paid order totals for revenue aggregation across standard reporting periods (UTC).
    /// </summary>
    Task<FinanceDashboardStatsRaw> GetFinanceAsync(
        DateTimeOffset utcDayStart,
        DateTimeOffset utcDayEndExclusive,
        DateTimeOffset utcMonthStart,
        DateTimeOffset utcYearStart,
        CancellationToken cancellationToken = default);
}
