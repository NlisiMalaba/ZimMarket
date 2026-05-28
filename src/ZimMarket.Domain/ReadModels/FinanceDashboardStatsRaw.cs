namespace ZimMarket.Domain.ReadModels;

/// <summary>Paid order totals by period before application-layer currency conversion to USD.</summary>
public sealed record FinanceDashboardStatsRaw(
    IReadOnlyList<PaidOrderTotalRow> PaidOrderTotalsToday,
    IReadOnlyList<PaidOrderTotalRow> PaidOrderTotalsMonth,
    IReadOnlyList<PaidOrderTotalRow> PaidOrderTotalsYear,
    IReadOnlyList<PaidOrderTotalRow> PaidOrderTotalsAllTime);
