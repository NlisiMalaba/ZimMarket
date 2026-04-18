using MediatR;
using Microsoft.Extensions.Logging;
using ZimMarket.Application.Common;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Application.Common.Models;
using ZimMarket.Domain.Enums;
using ZimMarket.Domain.Extensions;
using ZimMarket.Domain.Interfaces;

namespace ZimMarket.Application.Orders;

public sealed class CancelOrderCommandHandler : IRequestHandler<CancelOrderCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<CancelOrderCommandHandler> _logger;

    public CancelOrderCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        ILogger<CancelOrderCommandHandler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId == Guid.Empty || _currentUser.Role != UserRole.Customer)
        {
            _logger.LogDebug("Cancel order rejected: caller is not an authenticated customer.");
            return Result.Failure(
                OrderErrorCodes.OrderForbidden,
                "Only authenticated customers can cancel orders.");
        }

        var order = await _unitOfWork.Orders.GetByIdForUpdateAsync(request.OrderId, cancellationToken).ConfigureAwait(false);
        if (order is null)
        {
            return Result.Failure(OrderErrorCodes.OrderNotFound, "Order was not found.");
        }

        if (order.CustomerId != _currentUser.UserId)
        {
            return Result.Failure(OrderErrorCodes.OrderForbidden, "You can only cancel your own order.");
        }

        if (!order.Status.CanTransitionTo(OrderStatus.Cancelled))
        {
            return Result.Failure(OrderErrorCodes.OrderCannotCancel, $"Order in status '{order.Status}' cannot be cancelled.");
        }

        order.Cancel(request.Reason);

        foreach (var item in order.Items)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(item.ProductId, cancellationToken).ConfigureAwait(false);
            if (product is null)
            {
                _logger.LogWarning(
                    "Order {OrderId} cancellation stock restore skipped: product {ProductId} not found.",
                    order.Id,
                    item.ProductId);
                continue;
            }

            product.UpdateStock(item.Quantity);
            await _unitOfWork.Products.UpdateAsync(product, cancellationToken).ConfigureAwait(false);
        }

        await _unitOfWork.Orders.UpdateAsync(order, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
