using MediatR;
using Microsoft.Extensions.Logging;
using ZimMarket.Application.Common;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Application.Common.Models;
using ZimMarket.Application.Logistics;
using ZimMarket.Domain.Entities.Logistics;
using ZimMarket.Domain.Entities.Orders;
using ZimMarket.Domain.Entities.Users;
using ZimMarket.Domain.Enums;
using ZimMarket.Domain.Exceptions;
using ZimMarket.Domain.Interfaces;

namespace ZimMarket.Application.Drivers;

public sealed class ConfirmDeliveryCommandHandler : IRequestHandler<ConfirmDeliveryCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IFileStorage _fileStorage;
    private readonly ILogger<ConfirmDeliveryCommandHandler> _logger;

    public ConfirmDeliveryCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IFileStorage fileStorage,
        ILogger<ConfirmDeliveryCommandHandler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _fileStorage = fileStorage ?? throw new ArgumentNullException(nameof(fileStorage));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result> Handle(ConfirmDeliveryCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId == Guid.Empty || _currentUser.Role != UserRole.Driver)
        {
            _logger.LogDebug("Confirm delivery rejected: caller is not an authenticated driver.");
            return Result.Failure(
                DriverDeliveryErrorCodes.DriverForbidden,
                "Only authenticated drivers can confirm delivery.");
        }

        Guid driverId = _currentUser.UserId;
        string photoKey = request.DeliveryPhotoKey.Trim();

        Result? blobCheck = await CheckDeliveryPhotoExistsAsync(photoKey, cancellationToken).ConfigureAwait(false);
        if (blobCheck is not null)
            return blobCheck;

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

        if (batch.Status == DeliveryBatchStatus.Completed)
        {
            return Result.Failure(
                LogisticsErrorCodes.BatchNotReadyForDelivery,
                "This delivery batch is already completed.");
        }

        if (batch.Status == DeliveryBatchStatus.Created)
        {
            return Result.Failure(
                LogisticsErrorCodes.BatchNotReadyForDelivery,
                "The batch must be collected before deliveries can be confirmed.");
        }

        if (!batch.OrderIds.Contains(request.OrderId))
        {
            return Result.Failure(
                LogisticsErrorCodes.OrderNotInDeliveryBatch,
                "This order is not part of the specified batch.");
        }

        if (batch.Status == DeliveryBatchStatus.Collected)
        {
            try
            {
                batch.MarkInTransit();
            }
            catch (DomainException ex)
            {
                return Result.Failure(LogisticsErrorCodes.DeliveryBatchInvalidState, ex.Message);
            }
        }

        Order? order = await _unitOfWork.Orders
            .GetByIdForUpdateAsync(request.OrderId, cancellationToken)
            .ConfigureAwait(false);

        if (order is null)
        {
            return Result.Failure(
                OrderErrorCodes.OrderNotFound,
                "Order was not found.");
        }

        if (order.Status == OrderStatus.Delivered)
        {
            return Result.Failure(
                LogisticsErrorCodes.OrderAlreadyDelivered,
                "This order has already been marked as delivered.");
        }

        if (order.Status != OrderStatus.OutForDelivery)
        {
            return Result.Failure(
                LogisticsErrorCodes.OrderNotOutForDelivery,
                $"Order must be out for delivery. Current status: {order.Status}.");
        }

        try
        {
            order.ConfirmDelivered(photoKey);
        }
        catch (DomainException ex)
        {
            return Result.Failure(LogisticsErrorCodes.OrderNotOutForDelivery, ex.Message);
        }

        await _unitOfWork.Orders.UpdateAsync(order, cancellationToken).ConfigureAwait(false);

        bool allDelivered = true;
        foreach (Guid orderId in batch.OrderIds)
        {
            Order? o = await _unitOfWork.Orders
                .GetByIdForUpdateAsync(orderId, cancellationToken)
                .ConfigureAwait(false);

            if (o is null || o.Status != OrderStatus.Delivered)
            {
                allDelivered = false;
                break;
            }
        }

        if (allDelivered)
        {
            try
            {
                batch.Complete();
            }
            catch (DomainException ex)
            {
                return Result.Failure(LogisticsErrorCodes.DeliveryBatchInvalidState, ex.Message);
            }

            Driver? driver = await _unitOfWork.Drivers
                .GetByIdAsync(driverId, cancellationToken)
                .ConfigureAwait(false);

            if (driver is not null)
            {
                driver.SetStatus(DriverStatus.Available);
                await _unitOfWork.Drivers.UpdateAsync(driver, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                _logger.LogWarning(
                    "Batch {BatchId} completed but driver {DriverId} profile was not found for status reset.",
                    batch.Id,
                    driverId);
            }
        }

        await _unitOfWork.DeliveryBatches.UpdateAsync(batch, cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }

    private async Task<Result?> CheckDeliveryPhotoExistsAsync(string key, CancellationToken cancellationToken)
    {
        try
        {
            if (!await _fileStorage.ExistsAsync(key, cancellationToken).ConfigureAwait(false))
            {
                return Result.ValidationFailure(
                [
                    new ValidationError(
                        nameof(ConfirmDeliveryCommand.DeliveryPhotoKey),
                        "The delivery photo was not found in storage. Upload the file first.")
                ]);
            }
        }
        catch (ArgumentException ex)
        {
            return Result.ValidationFailure(
            [
                new ValidationError(nameof(ConfirmDeliveryCommand.DeliveryPhotoKey), ex.Message)
            ]);
        }

        return null;
    }
}
