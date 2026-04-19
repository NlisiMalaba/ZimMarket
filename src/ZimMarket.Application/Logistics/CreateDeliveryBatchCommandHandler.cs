using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZimMarket.Application.Common;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Application.Common.Models;
using ZimMarket.Domain.Entities.Logistics;
using ZimMarket.Domain.Entities.Orders;
using ZimMarket.Domain.Entities.Users;
using ZimMarket.Domain.Entities.Warehouse;
using ZimMarket.Domain.Enums;
using ZimMarket.Domain.Interfaces;

namespace ZimMarket.Application.Logistics;

public sealed class CreateDeliveryBatchCommandHandler : IRequestHandler<CreateDeliveryBatchCommand, Result<Guid>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IOptions<LogisticsOptions> _logisticsOptions;
    private readonly ILogger<CreateDeliveryBatchCommandHandler> _logger;

    public CreateDeliveryBatchCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IOptions<LogisticsOptions> logisticsOptions,
        ILogger<CreateDeliveryBatchCommandHandler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _logisticsOptions = logisticsOptions ?? throw new ArgumentNullException(nameof(logisticsOptions));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<Guid>> Handle(CreateDeliveryBatchCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated
            || _currentUser.UserId == Guid.Empty
            || (_currentUser.Role != UserRole.Admin && _currentUser.Role != UserRole.SuperAdmin))
        {
            _logger.LogDebug("Create delivery batch rejected: caller is not an admin.");
            return Result<Guid>.Failure(
                LogisticsErrorCodes.LogisticsForbidden,
                "Only administrators can create delivery batches.");
        }

        IReadOnlyList<Guid> sortedOrderIds = request.OrderIds.OrderBy(x => x).ToList();

        foreach (Guid orderId in sortedOrderIds)
        {
            Order? order = await _unitOfWork.Orders
                .GetByIdForUpdateAsync(orderId, cancellationToken)
                .ConfigureAwait(false);

            if (order is null)
            {
                return Result<Guid>.Failure(
                    OrderErrorCodes.OrderNotFound,
                    $"Order {orderId:D} was not found.");
            }

            if (order.Status != OrderStatus.QcPassed)
            {
                return Result<Guid>.Failure(
                    LogisticsErrorCodes.OrderNotEligibleForBatch,
                    $"Order {orderId:D} must be QC passed and unbatched. Current status: {order.Status}.");
            }

            DeliveryBatch? existing = await _unitOfWork.DeliveryBatches
                .GetByOrderIdAsync(orderId, cancellationToken)
                .ConfigureAwait(false);

            if (existing is not null)
            {
                return Result<Guid>.Failure(
                    LogisticsErrorCodes.OrderAlreadyBatched,
                    $"Order {orderId:D} is already assigned to a delivery batch.");
            }
        }

        Driver? driver = await _unitOfWork.Drivers
            .GetByIdAsync(request.DriverId, cancellationToken)
            .ConfigureAwait(false);

        if (driver is null)
        {
            return Result<Guid>.Failure(
                LogisticsErrorCodes.DriverNotFound,
                "Driver was not found.");
        }

        if (!driver.IsApproved || driver.KycStatus != KycStatus.Approved)
        {
            return Result<Guid>.Failure(
                LogisticsErrorCodes.DriverNotEligible,
                "Driver must be approved before assignment to a batch.");
        }

        if (driver.DriverStatus != DriverStatus.Available)
        {
            return Result<Guid>.Failure(
                LogisticsErrorCodes.DriverNotEligible,
                $"Driver must be available. Current status: {driver.DriverStatus}.");
        }

        DeliveryBatch? activeForDriver = await _unitOfWork.DeliveryBatches
            .GetActiveByDriverAsync(request.DriverId, cancellationToken)
            .ConfigureAwait(false);

        if (activeForDriver is not null)
        {
            return Result<Guid>.Failure(
                LogisticsErrorCodes.DriverHasActiveBatch,
                "Driver already has an active delivery batch.");
        }

        Guid warehouseId = _logisticsOptions.Value.DefaultPickupWarehouseId;
        if (warehouseId == Guid.Empty)
        {
            return Result<Guid>.Failure(
                LogisticsErrorCodes.BatchCreateFailed,
                "Default pickup warehouse is not configured.");
        }

        DateTimeOffset now = DateTimeOffset.UtcNow;
        Guid batchId = Guid.NewGuid();

        ZimMarket.Shared.Result<DeliveryBatch> batchCreate = DeliveryBatch.Create(
            batchId,
            request.DriverId,
            warehouseId,
            sortedOrderIds,
            now,
            now);

        if (batchCreate.IsFailure)
        {
            return Result<Guid>.Failure(
                LogisticsErrorCodes.BatchCreateFailed,
                string.Join("; ", batchCreate.Errors));
        }

        DeliveryBatch batch = batchCreate.Value!;

        foreach (Guid orderId in sortedOrderIds)
        {
            Order order = (await _unitOfWork.Orders
                .GetByIdForUpdateAsync(orderId, cancellationToken)
                .ConfigureAwait(false))!;

            order.UpdateStatus(OrderStatus.Batched);

            IReadOnlyList<WarehouseItem> lines = await _unitOfWork.WarehouseItems
                .GetByOrderIdForUpdateAsync(orderId, cancellationToken)
                .ConfigureAwait(false);

            foreach (WarehouseItem line in lines)
            {
                if (line.QcStatus == WarehouseQcStatus.Passed && line.BatchId is null)
                    line.AssignToDeliveryBatch(batchId);

                await _unitOfWork.WarehouseItems.UpdateAsync(line, cancellationToken).ConfigureAwait(false);
            }

            await _unitOfWork.Orders.UpdateAsync(order, cancellationToken).ConfigureAwait(false);
        }

        driver.SetStatus(DriverStatus.OnDelivery);
        await _unitOfWork.Drivers.UpdateAsync(driver, cancellationToken).ConfigureAwait(false);

        await _unitOfWork.DeliveryBatches.AddAsync(batch, cancellationToken).ConfigureAwait(false);

        return Result<Guid>.Success(batchId);
    }
}
