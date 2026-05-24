using MediatR;
using Microsoft.Extensions.Logging;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Domain.Enums;
using ZimMarket.Domain.Interfaces;
using ZimMarket.Domain.ReadModels;
using Models = ZimMarket.Application.Common.Models;

namespace ZimMarket.Application.Admin;

public sealed class GetFinanceDashboardStatsQueryHandler
    : IRequestHandler<GetFinanceDashboardStatsQuery, Models.Result<FinanceDashboardStatsDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IExchangeRateService _exchangeRateService;
    private readonly ILogger<GetFinanceDashboardStatsQueryHandler> _logger;

    public GetFinanceDashboardStatsQueryHandler(
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IExchangeRateService exchangeRateService,
        ILogger<GetFinanceDashboardStatsQueryHandler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _exchangeRateService = exchangeRateService ?? throw new ArgumentNullException(nameof(exchangeRateService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Models.Result<FinanceDashboardStatsDto>> Handle(
        GetFinanceDashboardStatsQuery request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated
            || _currentUser.UserId == Guid.Empty
            || _currentUser.Role != UserRole.SuperAdmin)
        {
            _logger.LogDebug("Get finance dashboard stats rejected: caller is not a super admin.");
            return Models.Result<FinanceDashboardStatsDto>.Failure(
                AdminDashboardErrorCodes.Forbidden,
                "Only super administrators can view financial dashboard statistics.");
        }

        DateTime utcNow = DateTime.UtcNow;
        DateTime utcDate = utcNow.Date;
        var utcDayStart = new DateTimeOffset(utcDate, TimeSpan.Zero);
        DateTimeOffset utcDayEndExclusive = utcDayStart.AddDays(1);
        var utcMonthStart = new DateTimeOffset(new DateTime(utcDate.Year, utcDate.Month, 1), TimeSpan.Zero);
        var utcYearStart = new DateTimeOffset(new DateTime(utcDate.Year, 1, 1), TimeSpan.Zero);

        FinanceDashboardStatsRaw raw = await _unitOfWork.DashboardStats
            .GetFinanceAsync(
                utcDayStart,
                utcDayEndExclusive,
                utcMonthStart,
                utcYearStart,
                cancellationToken)
            .ConfigureAwait(false);

        decimal zwgPerUsd = await _exchangeRateService.GetUsdToZwlAsync(cancellationToken).ConfigureAwait(false);

        Models.Result<decimal> today = AdminDashboardRevenueAggregator.SumPaidOrdersUsd(
            raw.PaidOrderTotalsToday,
            zwgPerUsd,
            _logger);
        if (!today.IsSuccess)
        {
            return Models.Result<FinanceDashboardStatsDto>.Failure(today.ErrorCode!, today.ErrorMessage!);
        }

        Models.Result<decimal> month = AdminDashboardRevenueAggregator.SumPaidOrdersUsd(
            raw.PaidOrderTotalsMonth,
            zwgPerUsd,
            _logger);
        if (!month.IsSuccess)
        {
            return Models.Result<FinanceDashboardStatsDto>.Failure(month.ErrorCode!, month.ErrorMessage!);
        }

        Models.Result<decimal> year = AdminDashboardRevenueAggregator.SumPaidOrdersUsd(
            raw.PaidOrderTotalsYear,
            zwgPerUsd,
            _logger);
        if (!year.IsSuccess)
        {
            return Models.Result<FinanceDashboardStatsDto>.Failure(year.ErrorCode!, year.ErrorMessage!);
        }

        Models.Result<decimal> allTime = AdminDashboardRevenueAggregator.SumPaidOrdersUsd(
            raw.PaidOrderTotalsAllTime,
            zwgPerUsd,
            _logger);
        if (!allTime.IsSuccess)
        {
            return Models.Result<FinanceDashboardStatsDto>.Failure(allTime.ErrorCode!, allTime.ErrorMessage!);
        }

        return Models.Result<FinanceDashboardStatsDto>.Success(
            new FinanceDashboardStatsDto(
                today.Value!,
                month.Value!,
                year.Value!,
                allTime.Value!));
    }
}
