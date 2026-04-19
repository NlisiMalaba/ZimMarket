using ZimMarket.Application.Common.Abstractions;
using ZimMarket.Application.Common.Models;

namespace ZimMarket.Application.Admin;

/// <summary>Admin dashboard aggregates for the current UTC calendar day. Cached for five minutes per day key.</summary>
public sealed record GetDashboardStatsQuery : IQuery<DashboardStatsDto>, ICacheable
{
    public string CacheKey => $"admin:dashboard-stats:{DateTime.UtcNow:yyyy-MM-dd}";

    public TimeSpan Ttl => TimeSpan.FromMinutes(5);
}
