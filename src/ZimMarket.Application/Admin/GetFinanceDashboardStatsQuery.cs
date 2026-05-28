using ZimMarket.Application.Common.Abstractions;
using ZimMarket.Application.Common.Models;

namespace ZimMarket.Application.Admin;

/// <summary>Super-admin revenue aggregates (today, month, year, all time). Cached for five minutes per UTC day.</summary>
public sealed record GetFinanceDashboardStatsQuery : IQuery<FinanceDashboardStatsDto>, ICacheable
{
    public string CacheKey => $"admin:dashboard-finance:{DateTime.UtcNow:yyyy-MM-dd}";

    public TimeSpan Ttl => TimeSpan.FromMinutes(5);
}
