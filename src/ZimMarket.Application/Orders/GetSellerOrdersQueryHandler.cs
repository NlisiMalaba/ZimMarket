using MediatR;
using ZimMarket.Application.Common;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Application.Common.Models;
using ZimMarket.Domain.Entities.Orders;
using ZimMarket.Domain.Enums;
using ZimMarket.Domain.Interfaces;

namespace ZimMarket.Application.Orders;

public sealed class GetSellerOrdersQueryHandler
    : IRequestHandler<GetSellerOrdersQuery, Result<ZimMarket.Shared.PagedList<SellerOrderListItemDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public GetSellerOrdersQueryHandler(
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
    }

    public async Task<Result<ZimMarket.Shared.PagedList<SellerOrderListItemDto>>> Handle(
        GetSellerOrdersQuery request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId == Guid.Empty || _currentUser.Role != UserRole.Seller)
        {
            return Result<ZimMarket.Shared.PagedList<SellerOrderListItemDto>>.Failure(
                OrderErrorCodes.OrderForbidden,
                "Only authenticated sellers can view seller orders.");
        }

        var pagination = new ZimMarket.Shared.PaginationParams
        {
            Page = request.Page,
            PageSize = request.PageSize
        };

        ZimMarket.Shared.PagedList<Order> orders = await _unitOfWork.Orders
            .GetBySellerPagedAsync(_currentUser.UserId, pagination, request.StatusFilter, cancellationToken)
            .ConfigureAwait(false);

        var items = new List<SellerOrderListItemDto>(orders.Items.Count);
        foreach (Order order in orders.Items)
        {
            int sellerLineItemCount = await CountSellerLineItemsAsync(order, _currentUser.UserId, cancellationToken).ConfigureAwait(false);
            if (sellerLineItemCount == 0)
                continue;

            items.Add(new SellerOrderListItemDto(
                order.Id,
                order.Status,
                order.PaymentStatus,
                order.TotalAmount.Amount,
                sellerLineItemCount,
                order.CreatedAt));
        }

        return Result<ZimMarket.Shared.PagedList<SellerOrderListItemDto>>.Success(
            new ZimMarket.Shared.PagedList<SellerOrderListItemDto>(
                items,
                orders.Page,
                orders.PageSize,
                orders.TotalCount));
    }

    private async Task<int> CountSellerLineItemsAsync(Order order, Guid sellerId, CancellationToken cancellationToken)
    {
        int count = 0;

        foreach (var item in order.Items)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(item.ProductId, cancellationToken).ConfigureAwait(false);
            if (product?.SellerId == sellerId)
                count++;
        }

        return count;
    }
}

