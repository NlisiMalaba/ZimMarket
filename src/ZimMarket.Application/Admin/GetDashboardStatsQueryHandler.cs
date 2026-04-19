using MediatR;
using Microsoft.Extensions.Logging;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Domain.Enums;
using ZimMarket.Domain.Interfaces;
using ZimMarket.Domain.ReadModels;
using ZimMarket.Domain.ValueObjects;
using Models = ZimMarket.Application.Common.Models;

namespace ZimMarket.Application.Admin;

public sealed class GetDashboardStatsQueryHandler
    : IRequestHandler<GetDashboardStatsQuery, Models.Result<DashboardStatsDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IExchangeRateService _exchangeRateService;
    private readonly ILogger<GetDashboardStatsQueryHandler> _logger;

    public GetDashboardStatsQueryHandler(
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IExchangeRateService exchangeRateService,
        ILogger<GetDashboardStatsQueryHandler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _exchangeRateService = exchangeRateService ?? throw new ArgumentNullException(nameof(exchangeRateService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Models.Result<DashboardStatsDto>> Handle(
        GetDashboardStatsQuery request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated
            || _currentUser.UserId == Guid.Empty
            || (_currentUser.Role != UserRole.Admin && _currentUser.Role != UserRole.SuperAdmin))
        {
            _logger.LogDebug("Get dashboard stats rejected: caller is not an admin or super admin.");
            return Models.Result<DashboardStatsDto>.Failure(
                AdminDashboardErrorCodes.Forbidden,
                "Only administrators or super administrators can view dashboard statistics.");
        }

        DateTime utcDate = DateTime.UtcNow.Date;
        var utcDayStart = new DateTimeOffset(utcDate, TimeSpan.Zero);
        DateTimeOffset utcDayEndExclusive = utcDayStart.AddDays(1);

        DashboardStatsRaw raw = await _unitOfWork.DashboardStats
            .GetAsync(
                utcDayStart,
                utcDayEndExclusive,
                AdminDashboardConstants.LowStockMaxQuantityInclusive,
                cancellationToken)
            .ConfigureAwait(false);

        decimal zwgPerUsd = await _exchangeRateService.GetUsdToZwlAsync(cancellationToken).ConfigureAwait(false);

        Models.Result<decimal> revenueUsd = SumRevenueTodayUsd(raw.PaidOrderTotalsToday, zwgPerUsd);
        if (!revenueUsd.IsSuccess)
        {
            return Models.Result<DashboardStatsDto>.Failure(revenueUsd.ErrorCode!, revenueUsd.ErrorMessage!);
        }

        return Models.Result<DashboardStatsDto>.Success(
            new DashboardStatsDto(
                raw.OrdersTodayCount,
                revenueUsd.Value!,
                raw.ActiveDriversCount,
                raw.PendingKycCount,
                raw.LowStockProductsCount));
    }

    private Models.Result<decimal> SumRevenueTodayUsd(IReadOnlyList<PaidOrderTotalRow> rows, decimal zwgPerUsd)
    {
        decimal sum = 0;
        foreach (PaidOrderTotalRow row in rows)
        {
            if (row.Currency == Currency.ZAR)
            {
                _logger.LogWarning(
                    "Skipping paid order total in ZAR for USD revenue aggregation (no ZAR→USD rate configured). Amount={Amount}.",
                    row.Amount);
                continue;
            }

            ZimMarket.Shared.Result<Money> money = Money.Create(row.Amount, row.Currency);
            if (money.IsFailure)
            {
                _logger.LogWarning(
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
