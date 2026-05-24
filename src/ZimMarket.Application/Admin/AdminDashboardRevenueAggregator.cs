using Microsoft.Extensions.Logging;
using ZimMarket.Domain.Enums;
using ZimMarket.Domain.ReadModels;
using ZimMarket.Domain.ValueObjects;
using Models = ZimMarket.Application.Common.Models;

namespace ZimMarket.Application.Admin;

internal static class AdminDashboardRevenueAggregator
{
    public static Models.Result<decimal> SumPaidOrdersUsd(
        IReadOnlyList<PaidOrderTotalRow> rows,
        decimal zwgPerUsd,
        ILogger logger)
    {
        decimal sum = 0;
        foreach (PaidOrderTotalRow row in rows)
        {
            if (row.Currency == Currency.ZAR)
            {
                logger.LogWarning(
                    "Skipping paid order total in ZAR for USD revenue aggregation (no ZAR→USD rate configured). Amount={Amount}.",
                    row.Amount);
                continue;
            }

            ZimMarket.Shared.Result<Money> money = Money.Create(row.Amount, row.Currency);
            if (money.IsFailure)
            {
                logger.LogWarning(
                    "Skipping invalid paid order total for revenue: {Errors}",
                    string.Join("; ", money.Errors));
                continue;
            }

            ZimMarket.Shared.Result<Money> inUsd = money.Value!.Currency == Currency.USD
                ? money
                : money.Value.ToUsd(zwgPerUsd);

            if (inUsd.IsFailure)
            {
                return Models.Result<decimal>.Failure(
                    AdminDashboardErrorCodes.RevenueAggregationFailed,
                    string.Join("; ", inUsd.Errors));
            }

            sum += inUsd.Value!.Amount;
        }

        return Models.Result<decimal>.Success(sum);
    }
}
