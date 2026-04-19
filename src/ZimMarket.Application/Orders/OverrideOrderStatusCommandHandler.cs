using MediatR;
using Microsoft.Extensions.Logging;
using ZimMarket.Application.Common;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Application.Common.Models;
using ZimMarket.Domain.Entities.Orders;
using ZimMarket.Domain.Enums;
using ZimMarket.Domain.Exceptions;
using ZimMarket.Domain.Interfaces;

namespace ZimMarket.Application.Orders;

public sealed class OverrideOrderStatusCommandHandler : IRequestHandler<OverrideOrderStatusCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<OverrideOrderStatusCommandHandler> _logger;

    public OverrideOrderStatusCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        ILogger<OverrideOrderStatusCommandHandler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result> Handle(OverrideOrderStatusCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated
            || _currentUser.UserId == Guid.Empty
            || (_currentUser.Role != UserRole.Admin && _currentUser.Role != UserRole.SuperAdmin))
        {
            _logger.LogDebug("Override order status rejected: caller is not an admin or super admin.");
            return Result.Failure(
                AdminOrderErrorCodes.Forbidden,
                "Only administrators or super administrators can override order status.");
        }

        Order? order = await _unitOfWork.Orders
            .GetByIdForUpdateAsync(request.OrderId, cancellationToken)
            .ConfigureAwait(false);

        if (order is null)
        {
            _logger.LogWarning("Override order status: order {OrderId} was not found.", request.OrderId);
            return Result.Failure(OrderErrorCodes.OrderNotFound, "Order was not found.");
        }

        OrderStatus previous = order.Status;

        try
        {
            order.OverrideStatusByAdmin(request.NewStatus, request.Reason);
        }
        catch (DomainException ex)
        {
            _logger.LogDebug(ex, "Override order status rejected by domain rules for order {OrderId}.", request.OrderId);
            return Result.Failure(AdminOrderErrorCodes.CannotOverride, ex.Message);
        }

        if (order.Status == previous)
        {
            _logger.LogInformation(
                "Admin {AdminId} requested status override for order {OrderId} but status was already {Status}; no change.",
                _currentUser.UserId,
                request.OrderId,
                previous);
            return Result.Success();
        }

        await _unitOfWork.Orders.UpdateAsync(order, cancellationToken).ConfigureAwait(false);

        _logger.LogWarning(
            "Admin {AdminId} overrode order {OrderId} status from {PreviousStatus} to {NewStatus}. Reason: {Reason}",
            _currentUser.UserId,
            request.OrderId,
            previous,
            order.Status,
            request.Reason.Trim());

        return Result.Success();
    }
}
