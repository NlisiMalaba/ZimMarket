using MediatR;
using Microsoft.Extensions.Logging;
using ZimMarket.Application.Common;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Application.Common.Models;
using ZimMarket.Domain.Enums;
using ZimMarket.Domain.Interfaces;
using ZimMarket.Domain.ReadModels;

namespace ZimMarket.Application.Orders;

public sealed class GetAllOrdersQueryHandler
    : IRequestHandler<GetAllOrdersQuery, Result<ZimMarket.Shared.PagedList<AdminOrderListItemDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<GetAllOrdersQueryHandler> _logger;

    public GetAllOrdersQueryHandler(
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        ILogger<GetAllOrdersQueryHandler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<ZimMarket.Shared.PagedList<AdminOrderListItemDto>>> Handle(
        GetAllOrdersQuery request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated
            || _currentUser.UserId == Guid.Empty
            || (_currentUser.Role != UserRole.Admin && _currentUser.Role != UserRole.SuperAdmin))
        {
            _logger.LogDebug("Get all orders rejected: caller is not an admin or super admin.");
            return Result<ZimMarket.Shared.PagedList<AdminOrderListItemDto>>.Failure(
                AdminOrderErrorCodes.Forbidden,
                "Only administrators or super administrators can list all orders.");
        }

        var pagination = new ZimMarket.Shared.PaginationParams
        {
            Page = request.Page,
            PageSize = request.PageSize
        };

        ZimMarket.Shared.PagedList<OrderListAdminRow> page = await _unitOfWork.Orders
            .GetAllPagedForAdminAsync(request.Status, request.DateFrom, request.DateTo, pagination, cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<AdminOrderListItemDto> items = page.Items
            .Select(r => new AdminOrderListItemDto(
                r.OrderId,
                r.CustomerId,
                r.Status,
                r.PaymentStatus,
                r.TotalAmount,
                r.TotalCurrency.ToString(),
                r.LineItemCount,
                r.CreatedAt))
            .ToList();

        return Result<ZimMarket.Shared.PagedList<AdminOrderListItemDto>>.Success(
            new ZimMarket.Shared.PagedList<AdminOrderListItemDto>(items, page.Page, page.PageSize, page.TotalCount));
    }
}
