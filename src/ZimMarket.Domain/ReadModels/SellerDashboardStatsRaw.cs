namespace ZimMarket.Domain.ReadModels;

public sealed record SellerDashboardStatsRaw(
    int TotalOrders,
    decimal TotalRevenueUsd,
    int ActiveListings,
    int LowStockCount);
