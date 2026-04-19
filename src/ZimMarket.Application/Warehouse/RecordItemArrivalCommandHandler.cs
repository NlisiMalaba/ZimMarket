using MediatR;
using Microsoft.Extensions.Logging;
using ZimMarket.Application.Common;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Domain.Entities.Warehouse;
using ZimMarket.Domain.Enums;
using ZimMarket.Domain.Interfaces;
using ZimMarket.Shared;
using AppResult = ZimMarket.Application.Common.Models.Result;

namespace ZimMarket.Application.Warehouse;

public sealed class RecordItemArrivalCommandHandler : IRequestHandler<RecordItemArrivalCommand, AppResult>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<RecordItemArrivalCommandHandler> _logger;

    public RecordItemArrivalCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        ILogger<RecordItemArrivalCommandHandler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<AppResult> Handle(RecordItemArrivalCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated
            || _currentUser.UserId == Guid.Empty
            || (_currentUser.Role != UserRole.Admin && _currentUser.Role != UserRole.SuperAdmin))
        {
            _logger.LogDebug("Record item arrival rejected: caller is not an admin.");
            return AppResult.Failure(
                OrderErrorCodes.OrderForbidden,
                "Only administrators can record warehouse arrivals.");
        }

        var order = await _unitOfWork.Orders
            .GetByIdForUpdateAsync(request.OrderId, cancellationToken)
            .ConfigureAwait(false);

        if (order is null)
            return AppResult.Failure(OrderErrorCodes.OrderNotFound, "Order was not found.");

        if (order.Status != OrderStatus.Paid)
        {
            return AppResult.Failure(
                OrderErrorCodes.OrderInvalidStatusForArrival,
                $"Arrival can only be recorded for paid orders. Current status: {order.Status}.");
        }

        IReadOnlyList<WarehouseItem> existing = await _unitOfWork.WarehouseItems
            .GetByOrderIdAsync(order.Id, cancellationToken)
            .ConfigureAwait(false);

        if (existing.Count > 0)
        {
            return AppResult.Failure(
                OrderErrorCodes.OrderArrivalAlreadyRecorded,
                "Warehouse arrival has already been recorded for this order.");
        }

        string? notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
        DateTimeOffset now = DateTimeOffset.UtcNow;
        Guid? firstWarehouseItemId = null;

        foreach (var line in order.Items)
        {
            Guid warehouseItemId = Guid.NewGuid();
            Result<WarehouseItem> created = WarehouseItem.Create(
                warehouseItemId,
                order.Id,
                line.ProductId,
                now,
                now,
                now,
                notes);

            if (created.IsFailure)
            {
                return AppResult.Failure(
                    OrderErrorCodes.OrderCreateFailed,
                    string.Join("; ", created.Errors));
            }

            firstWarehouseItemId ??= warehouseItemId;
            await _unitOfWork.WarehouseItems
                .AddAsync(created.Value!, cancellationToken)
                .ConfigureAwait(false);
        }

        order.MarkArrivedAtWarehouse(firstWarehouseItemId!.Value);
        await _unitOfWork.Orders.UpdateAsync(order, cancellationToken).ConfigureAwait(false);

        return AppResult.Success();
    }
}
