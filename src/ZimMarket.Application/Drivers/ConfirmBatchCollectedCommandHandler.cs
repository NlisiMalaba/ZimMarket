using MediatR;
using Microsoft.Extensions.Logging;
using ZimMarket.Application.Common;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Application.Common.Models;
using ZimMarket.Application.Logistics;
using ZimMarket.Domain.Entities.Logistics;
using ZimMarket.Domain.Entities.Orders;
using ZimMarket.Domain.Enums;
using ZimMarket.Domain.Exceptions;
using ZimMarket.Domain.Interfaces;

namespace ZimMarket.Application.Drivers;

public sealed class ConfirmBatchCollectedCommandHandler : IRequestHandler<ConfirmBatchCollectedCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<ConfirmBatchCollectedCommandHandler> _logger;

    public ConfirmBatchCollectedCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        ILogger<ConfirmBatchCollectedCommandHandler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result> Handle(ConfirmBatchCollectedCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId == Guid.Empty || _currentUser.Role != UserRole.Driver)
        {
            _logger.LogDebug("Confirm batch collected rejected: caller is not an authenticated driver.");
            return Result.Failure(
                DriverDeliveryErrorCodes.DriverForbidden,
                "Only authenticated drivers can confirm batch collection.");
        }

        Guid driverId = _currentUser.UserId;

        DeliveryBatch? batch = await _unitOfWork.DeliveryBatches
            .GetByIdForUpdateAsync(request.BatchId, cancellationToken)
            .ConfigureAwait(false);

        if (batch is null)
        {
            return Result.Failure(
                LogisticsErrorCodes.DeliveryBatchNotFound,
                "Delivery batch was not found.");
        }

        if (batch.DriverId != driverId)
        {
            return Result.Failure(
                LogisticsErrorCodes.DeliveryBatchForbidden,
                "This delivery batch is not assigned to you.");
        }

        if (batch.Status != DeliveryBatchStatus.Created)
        {
            return Result.Failure(
                LogisticsErrorCodes.DeliveryBatchInvalidState,
                $"The batch cannot be marked collected in its current state: {batch.Status}.");
        }

        var ordersToUpdate = new List<Order>();
        foreach (Guid orderId in batch.OrderIds)
        {
            Order? order = await _unitOfWork.Orders
                .GetByIdForUpdateAsync(orderId, cancellationToken)
                .ConfigureAwait(false);

            if (order is null)
            {
                return Result.Failure(
                    OrderErrorCodes.OrderNotFound,
                    $"Order {orderId:D} in the batch was not found.");
            }

            if (order.Status != OrderStatus.Batched)
            {
                return Result.Failure(
                    LogisticsErrorCodes.OrderNotBatchedForCollection,
                    $"Order {orderId:D} must be batched before collection. Current status: {order.Status}.");
            }

            ordersToUpdate.Add(order);
        }

        try
        {
            batch.MarkCollected();
        }
        catch (DomainException ex)
        {
            return Result.Failure(LogisticsErrorCodes.DeliveryBatchInvalidState, ex.Message);
        }

        foreach (Order order in ordersToUpdate)
        {
            order.UpdateStatus(OrderStatus.OutForDelivery);
            await _unitOfWork.Orders.UpdateAsync(order, cancellationToken).ConfigureAwait(false);
        }

        await _unitOfWork.DeliveryBatches.UpdateAsync(batch, cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
