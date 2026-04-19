using MediatR;
using Microsoft.Extensions.Logging;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Domain.Entities.Logistics;
using ZimMarket.Domain.Enums;
using ZimMarket.Domain.Interfaces;
using ZimMarket.Shared;

namespace ZimMarket.Application.Logistics;

public sealed class GetBatchesQueryHandler
    : IRequestHandler<GetBatchesQuery, ZimMarket.Application.Common.Models.Result<PagedList<DeliveryBatchListItemDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<GetBatchesQueryHandler> _logger;

    public GetBatchesQueryHandler(
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        ILogger<GetBatchesQueryHandler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ZimMarket.Application.Common.Models.Result<PagedList<DeliveryBatchListItemDto>>> Handle(
        GetBatchesQuery request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated
            || _currentUser.UserId == Guid.Empty
            || (_currentUser.Role != UserRole.Admin && _currentUser.Role != UserRole.SuperAdmin))
        {
            _logger.LogDebug("Get batches rejected: caller is not an admin.");
            return ZimMarket.Application.Common.Models.Result<PagedList<DeliveryBatchListItemDto>>.Failure(
                LogisticsErrorCodes.LogisticsForbidden,
                "Only administrators can list delivery batches.");
        }

        var pagination = new PaginationParams
        {
            Page = request.Page,
            PageSize = request.PageSize
        };

        PagedList<DeliveryBatch> page = await _unitOfWork.DeliveryBatches
            .GetPagedAsync(request.Status, pagination, cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<DeliveryBatchListItemDto> items = page.Items
            .Select(b => new DeliveryBatchListItemDto(
                b.Id,
                b.DriverId,
                b.WarehouseId,
                b.Status,
                b.OrderIds.Count,
                b.CreatedAt,
                b.UpdatedAt))
            .ToList();

        return ZimMarket.Application.Common.Models.Result<PagedList<DeliveryBatchListItemDto>>.Success(
            new PagedList<DeliveryBatchListItemDto>(items, page.Page, page.PageSize, page.TotalCount));
    }
}
