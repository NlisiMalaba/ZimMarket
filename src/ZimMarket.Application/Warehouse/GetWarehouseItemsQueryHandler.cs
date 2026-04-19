using MediatR;
using Microsoft.Extensions.Logging;
using ZimMarket.Application.Common;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Application.Common.Models;
using ZimMarket.Domain.Enums;
using ZimMarket.Domain.Interfaces;
using ZimMarket.Domain.ReadModels;

namespace ZimMarket.Application.Warehouse;

public sealed class GetWarehouseItemsQueryHandler
    : IRequestHandler<GetWarehouseItemsQuery, Result<ZimMarket.Shared.PagedList<WarehouseItemListItemDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<GetWarehouseItemsQueryHandler> _logger;

    public GetWarehouseItemsQueryHandler(
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        ILogger<GetWarehouseItemsQueryHandler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<ZimMarket.Shared.PagedList<WarehouseItemListItemDto>>> Handle(
        GetWarehouseItemsQuery request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated
            || _currentUser.UserId == Guid.Empty
            || (_currentUser.Role != UserRole.Admin && _currentUser.Role != UserRole.SuperAdmin))
        {
            _logger.LogDebug("Get warehouse items rejected: caller is not an admin.");
            return Result<ZimMarket.Shared.PagedList<WarehouseItemListItemDto>>.Failure(
                WarehouseErrorCodes.WarehouseForbidden,
                "Only administrators can list warehouse items.");
        }

        var pagination = new ZimMarket.Shared.PaginationParams
        {
            Page = request.Page,
            PageSize = request.PageSize
        };

        ZimMarket.Shared.PagedList<WarehouseItemListRow> page = await _unitOfWork.WarehouseItems
            .GetPagedForAdminAsync(request.QcStatus, pagination, cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<WarehouseItemListItemDto> items = page.Items.Select(WarehouseItemListItemDto.FromRow).ToList();

        return Result<ZimMarket.Shared.PagedList<WarehouseItemListItemDto>>.Success(
            new ZimMarket.Shared.PagedList<WarehouseItemListItemDto>(items, page.Page, page.PageSize, page.TotalCount));
    }
}
