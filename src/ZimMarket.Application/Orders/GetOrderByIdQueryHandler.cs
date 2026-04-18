using MediatR;
using ZimMarket.Application.Common;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Application.Common.Models;
using ZimMarket.Domain.Enums;
using ZimMarket.Domain.Interfaces;

namespace ZimMarket.Application.Orders;

public sealed class GetOrderByIdQueryHandler : IRequestHandler<GetOrderByIdQuery, Result<OrderDetailDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public GetOrderByIdQueryHandler(
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
    }

    public async Task<Result<OrderDetailDto>> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId == Guid.Empty)
        {
            return Result<OrderDetailDto>.Failure(
                OrderErrorCodes.OrderForbidden,
                "Authentication is required.");
        }

        var order = await _unitOfWork.Orders.GetByIdWithItemsAsync(request.OrderId, cancellationToken).ConfigureAwait(false);
        if (order is null)
        {
            return Result<OrderDetailDto>.Failure(OrderErrorCodes.OrderNotFound, "Order was not found.");
        }

        bool canView = _currentUser.Role switch
        {
            UserRole.Customer => order.CustomerId == _currentUser.UserId,
            UserRole.Admin or UserRole.SuperAdmin => true,
            UserRole.Seller => await IsSellerRelevantOrderAsync(order.Items.Select(x => x.ProductId), cancellationToken).ConfigureAwait(false),
            _ => false
        };

        if (!canView)
        {
            return Result<OrderDetailDto>.Failure(
                OrderErrorCodes.OrderForbidden,
                "You are not allowed to view this order.");
        }

        var deliveryBatch = await _unitOfWork.DeliveryBatches.GetByOrderIdAsync(order.Id, cancellationToken).ConfigureAwait(false);

        var items = order.Items
            .Select(x => new OrderDetailItemDto(
                x.ProductId,
                x.ProductTitle,
                x.Quantity,
                x.UnitPrice.Amount,
                x.LineTotal.Amount))
            .ToList();

        return Result<OrderDetailDto>.Success(
            new OrderDetailDto(
                order.Id,
                order.Status,
                order.PaymentStatus,
                deliveryBatch?.Id,
                items,
                order.TotalAmount.Amount));
    }

    private async Task<bool> IsSellerRelevantOrderAsync(IEnumerable<Guid> productIds, CancellationToken cancellationToken)
    {
        foreach (Guid productId in productIds.Distinct())
        {
            var product = await _unitOfWork.Products.GetByIdAsync(productId, cancellationToken).ConfigureAwait(false);
            if (product?.SellerId == _currentUser.UserId)
                return true;
        }

        return false;
    }
}
