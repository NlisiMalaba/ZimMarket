using MediatR;
using ZimMarket.Application.Common;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Application.Common.Models;
using ZimMarket.Domain.Enums;
using ZimMarket.Domain.Interfaces;

namespace ZimMarket.Application.Orders;

public sealed class GetCustomerOrdersQueryHandler
    : IRequestHandler<GetCustomerOrdersQuery, Result<ZimMarket.Shared.PagedList<CustomerOrderListItemDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public GetCustomerOrdersQueryHandler(
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
    }

    public async Task<Result<ZimMarket.Shared.PagedList<CustomerOrderListItemDto>>> Handle(
        GetCustomerOrdersQuery request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId == Guid.Empty || _currentUser.Role != UserRole.Customer)
        {
            return Result<ZimMarket.Shared.PagedList<CustomerOrderListItemDto>>.Failure(
                OrderErrorCodes.OrderForbidden,
                "Only authenticated customers can view customer orders.");
        }

        var pagination = new ZimMarket.Shared.PaginationParams
        {
            Page = request.Page,
            PageSize = request.PageSize
        };

        var orders = await _unitOfWork.Orders
            .GetByCustomerPagedAsync(_currentUser.UserId, pagination, request.StatusFilter, cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<CustomerOrderListItemDto> items = orders.Items
            .Select(x => new CustomerOrderListItemDto(
                x.Id,
                x.Status,
                x.PaymentStatus,
                x.TotalAmount.Amount,
                x.CreatedAt))
            .ToList();

        return Result<ZimMarket.Shared.PagedList<CustomerOrderListItemDto>>.Success(
            new ZimMarket.Shared.PagedList<CustomerOrderListItemDto>(
                items,
                orders.Page,
                orders.PageSize,
                orders.TotalCount));
    }
}
