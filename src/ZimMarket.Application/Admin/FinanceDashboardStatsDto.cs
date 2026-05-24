namespace ZimMarket.Application.Admin;

/// <summary>Revenue aggregates in USD for super administrators.</summary>
public sealed record FinanceDashboardStatsDto(
    decimal RevenueTodayUsd,
    decimal RevenueMonthUsd,
    decimal RevenueYearUsd,
    decimal RevenueAllTimeUsd);
