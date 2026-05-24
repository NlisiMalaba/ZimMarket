namespace ZimMarket.Domain.ReadModels;

/// <summary>Raw operational aggregates for the admin dashboard (no revenue).</summary>
public sealed record DashboardStatsRaw(
    int OrdersTodayCount,
    int PendingSellersCount,
    int PendingDriversCount,
    int ActiveDriversCount,
    int LowStockProductsCount);
