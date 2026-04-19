namespace ZimMarket.Application.Admin;

public sealed record DashboardStatsDto(
    int OrdersToday,
    decimal RevenueTodayUsd,
    int ActiveDrivers,
    int PendingKycCount,
    int LowStockProducts);
