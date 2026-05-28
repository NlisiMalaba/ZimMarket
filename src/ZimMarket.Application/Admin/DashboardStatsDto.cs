namespace ZimMarket.Application.Admin;

/// <summary>Operational admin dashboard metrics (all administrators).</summary>
public sealed record DashboardStatsDto(
    int OrdersToday,
    int PendingSellers,
    int PendingDrivers,
    int ActiveDrivers,
    int LowStockProducts);
