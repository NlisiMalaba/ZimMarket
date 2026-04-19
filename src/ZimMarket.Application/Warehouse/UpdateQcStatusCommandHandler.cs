using MediatR;
using Microsoft.Extensions.Logging;
using ZimMarket.Application.Common;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Domain.Entities.Warehouse;
using ZimMarket.Domain.Enums;
using ZimMarket.Domain.Exceptions;
using ZimMarket.Domain.Interfaces;
using AppResult = ZimMarket.Application.Common.Models.Result;

namespace ZimMarket.Application.Warehouse;

public sealed class UpdateQcStatusCommandHandler : IRequestHandler<UpdateQcStatusCommand, AppResult>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<UpdateQcStatusCommandHandler> _logger;

    public UpdateQcStatusCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        ILogger<UpdateQcStatusCommandHandler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<AppResult> Handle(UpdateQcStatusCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated
            || _currentUser.UserId == Guid.Empty
            || (_currentUser.Role != UserRole.Admin && _currentUser.Role != UserRole.SuperAdmin))
        {
            _logger.LogDebug("Update QC rejected: caller is not an admin.");
            return AppResult.Failure(
                WarehouseErrorCodes.WarehouseForbidden,
                "Only administrators can update warehouse QC.");
        }

        WarehouseItem? item = await _unitOfWork.WarehouseItems
            .GetByIdForUpdateAsync(request.WarehouseItemId, cancellationToken)
            .ConfigureAwait(false);

        if (item is null)
        {
            return AppResult.Failure(
                WarehouseErrorCodes.WarehouseItemNotFound,
                "Warehouse item was not found.");
        }

        var order = await _unitOfWork.Orders
            .GetByIdForUpdateAsync(item.OrderId, cancellationToken)
            .ConfigureAwait(false);

        if (order is null)
        {
            return AppResult.Failure(
                OrderErrorCodes.OrderNotFound,
                "Order was not found for this warehouse item.");
        }

        if (order.Status != OrderStatus.AtWarehouse)
        {
            return AppResult.Failure(
                WarehouseErrorCodes.OrderInvalidStatusForQc,
                $"QC can only be updated while the order is at the warehouse. Current status: {order.Status}.");
        }

        bool replaceNotes = request.Notes is not null;

        try
        {
            item.ApplyQcOutcome(request.QcStatus, replaceNotes, request.Notes);
        }
        catch (DomainException ex)
        {
            return AppResult.Failure(WarehouseErrorCodes.WarehouseQcInvalid, ex.Message);
        }

        if (request.QcStatus == WarehouseQcStatus.Passed)
        {
            IReadOnlyList<WarehouseItem> allForOrder = await _unitOfWork.WarehouseItems
                .GetByOrderIdForUpdateAsync(item.OrderId, cancellationToken)
                .ConfigureAwait(false);

            if (allForOrder.All(x => x.QcStatus == WarehouseQcStatus.Passed))
                order.UpdateStatus(OrderStatus.QcPassed);
        }

        await _unitOfWork.WarehouseItems.UpdateAsync(item, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.Orders.UpdateAsync(order, cancellationToken).ConfigureAwait(false);

        return AppResult.Success();
    }
}
