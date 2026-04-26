using MediatR;
using ZimMarket.Application.Common;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Application.Common.Models;
using ZimMarket.Domain.Enums;
using ZimMarket.Domain.Interfaces;

namespace ZimMarket.Application.Orders;

public sealed class GetSellerOrderDetailQueryHandler : IRequestHandler<GetSellerOrderDetailQuery, Result<SellerOrderDetailDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public GetSellerOrderDetailQueryHandler(
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
    }

    public async Task<Result<SellerOrderDetailDto>> Handle(GetSellerOrderDetailQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId == Guid.Empty || _currentUser.Role != UserRole.Seller)
        {
            return Result<SellerOrderDetailDto>.Failure(
                OrderErrorCodes.OrderForbidden,
                "Only authenticated sellers can view seller order details.");
        }

        var order = await _unitOfWork.Orders.GetByIdWithItemsAsync(request.OrderId, cancellationToken).ConfigureAwait(false);
        if (order is null)
            return Result<SellerOrderDetailDto>.Failure(OrderErrorCodes.OrderNotFound, "Order was not found.");

        var sellerItems = new List<SellerOrderDetailItemDto>();
        foreach (var item in order.Items)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(item.ProductId, cancellationToken).ConfigureAwait(false);
            if (product?.SellerId != _currentUser.UserId)
                continue;

            sellerItems.Add(new SellerOrderDetailItemDto(
                item.ProductId,
                item.ProductTitle,
                item.Quantity,
                item.UnitPrice.Amount,
                item.LineTotal.Amount));
        }

        if (sellerItems.Count == 0)
        {
            return Result<SellerOrderDetailDto>.Failure(
                OrderErrorCodes.OrderForbidden,
                "You are not allowed to view this order.");
        }

        return Result<SellerOrderDetailDto>.Success(new SellerOrderDetailDto(
            order.Id,
            order.Status,
            order.PaymentStatus,
            order.TotalAmount.Amount,
            order.DeliveryAddress.City,
            sellerItems));
    }
}

