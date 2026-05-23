namespace ZimMarket.Application.Sellers;

public sealed record SellerDashboardStatsDto(
    int TotalOrders,
    decimal TotalRevenueUsd,
    int ActiveListings,
    int LowStockCount);
