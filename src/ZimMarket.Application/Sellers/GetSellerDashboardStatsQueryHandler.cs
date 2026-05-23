using MediatR;
using ZimMarket.Application.Admin;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Application.Common.Models;
using ZimMarket.Domain.Enums;
using ZimMarket.Domain.Interfaces;
using ZimMarket.Domain.ReadModels;

namespace ZimMarket.Application.Sellers;

public sealed class GetSellerDashboardStatsQueryHandler
    : IRequestHandler<GetSellerDashboardStatsQuery, Result<SellerDashboardStatsDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public GetSellerDashboardStatsQueryHandler(IUnitOfWork unitOfWork, ICurrentUser currentUser)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
    }

    public async Task<Result<SellerDashboardStatsDto>> Handle(
        GetSellerDashboardStatsQuery request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated
            || _currentUser.UserId == Guid.Empty
            || _currentUser.Role != UserRole.Seller)
        {
            return Result<SellerDashboardStatsDto>.Failure(
                "SellerDashboard.Forbidden",
                "Only authenticated sellers can view dashboard statistics.");
        }

        SellerDashboardStatsRaw raw = await _unitOfWork.SellerDashboard
            .GetAsync(
                _currentUser.UserId,
                AdminDashboardConstants.LowStockMaxQuantityInclusive,
                cancellationToken)
            .ConfigureAwait(false);

        return Result<SellerDashboardStatsDto>.Success(
            new SellerDashboardStatsDto(
                raw.TotalOrders,
                raw.TotalRevenueUsd,
                raw.ActiveListings,
                raw.LowStockCount));
    }
}
