using MediatR;
using Microsoft.Extensions.Logging;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Application.Common.Models;
using ZimMarket.Domain.Entities.Logistics;
using ZimMarket.Domain.Enums;
using ZimMarket.Domain.Interfaces;

namespace ZimMarket.Application.Logistics;

public sealed class GetBatchDetailsQueryHandler
    : IRequestHandler<GetBatchDetailsQuery, Result<DeliveryBatchDetailDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<GetBatchDetailsQueryHandler> _logger;

    public GetBatchDetailsQueryHandler(
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        ILogger<GetBatchDetailsQueryHandler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<DeliveryBatchDetailDto>> Handle(
        GetBatchDetailsQuery request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId == Guid.Empty)
        {
            _logger.LogDebug("Get batch details rejected: caller is not authenticated.");
            return Result<DeliveryBatchDetailDto>.Failure(
                LogisticsErrorCodes.LogisticsForbidden,
                "Authentication is required.");
        }

        DeliveryBatch? batch = await _unitOfWork.DeliveryBatches
            .GetByIdAsync(request.BatchId, cancellationToken)
            .ConfigureAwait(false);

        if (batch is null)
        {
            return Result<DeliveryBatchDetailDto>.Failure(
                LogisticsErrorCodes.DeliveryBatchNotFound,
                "Delivery batch was not found.");
        }

        bool isAdmin = _currentUser.Role is UserRole.Admin or UserRole.SuperAdmin;
        bool isAssignedDriver =
            _currentUser.Role == UserRole.Driver && batch.DriverId == _currentUser.UserId;

        if (!isAdmin && !isAssignedDriver)
        {
            _logger.LogDebug(
                "Get batch details rejected: caller cannot access batch {BatchId}.",
                request.BatchId);
            return Result<DeliveryBatchDetailDto>.Failure(
                LogisticsErrorCodes.DeliveryBatchForbidden,
                "You are not allowed to view this delivery batch.");
        }

        IReadOnlyList<Guid> orderIds = batch.OrderIds.ToArray();

        var dto = new DeliveryBatchDetailDto(
            batch.Id,
            batch.DriverId,
            batch.WarehouseId,
            batch.Status,
            orderIds,
            batch.CollectedAt,
            batch.CompletedAt,
            batch.CreatedAt,
            batch.UpdatedAt);

        return Result<DeliveryBatchDetailDto>.Success(dto);
    }
}
