namespace ZimMarket.Domain.ReadModels;

/// <summary>Raw aggregates for the admin dashboard before application-layer currency conversion.</summary>
public sealed record DashboardStatsRaw(
    int OrdersTodayCount,
    IReadOnlyList<PaidOrderTotalRow> PaidOrderTotalsToday,
    int PendingKycCount,
    int ActiveDriversCount,
    int LowStockProductsCount);
