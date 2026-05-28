using MediatR;
using Microsoft.Extensions.Logging;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Domain.Enums;
using ZimMarket.Domain.Interfaces;
using ZimMarket.Domain.ReadModels;
using Models = ZimMarket.Application.Common.Models;

namespace ZimMarket.Application.Admin;

public sealed class GetDashboardStatsQueryHandler
    : IRequestHandler<GetDashboardStatsQuery, Models.Result<DashboardStatsDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<GetDashboardStatsQueryHandler> _logger;

    public GetDashboardStatsQueryHandler(
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        ILogger<GetDashboardStatsQueryHandler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
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
            .GetOperationalAsync(
                utcDayStart,
                utcDayEndExclusive,
                AdminDashboardConstants.LowStockMaxQuantityInclusive,
                cancellationToken)
            .ConfigureAwait(false);

        return Models.Result<DashboardStatsDto>.Success(
            new DashboardStatsDto(
                raw.OrdersTodayCount,
                raw.PendingSellersCount,
                raw.PendingDriversCount,
                raw.ActiveDriversCount,
                raw.LowStockProductsCount));
    }
}
