using MediatR;
using Microsoft.Extensions.Logging;
using ZimMarket.Application.Common;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Application.Common.Models;
using ZimMarket.Domain.Enums;
using ZimMarket.Domain.Interfaces;
using ZimMarket.Domain.ReadModels;

namespace ZimMarket.Application.Warehouse;

public sealed class GetUnbatchedItemsQueryHandler
    : IRequestHandler<GetUnbatchedItemsQuery, Result<IReadOnlyList<WarehouseItemListItemDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<GetUnbatchedItemsQueryHandler> _logger;

    public GetUnbatchedItemsQueryHandler(
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        ILogger<GetUnbatchedItemsQueryHandler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<IReadOnlyList<WarehouseItemListItemDto>>> Handle(
        GetUnbatchedItemsQuery request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated
            || _currentUser.UserId == Guid.Empty
            || (_currentUser.Role != UserRole.Admin && _currentUser.Role != UserRole.SuperAdmin))
        {
            _logger.LogDebug("Get unbatched warehouse items rejected: caller is not an admin.");
            return Result<IReadOnlyList<WarehouseItemListItemDto>>.Failure(
                WarehouseErrorCodes.WarehouseForbidden,
                "Only administrators can list unbatched warehouse items.");
        }

        IReadOnlyList<WarehouseItemListRow> rows = await _unitOfWork.WarehouseItems
            .GetUnbatchedWithOrderAsync(cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<WarehouseItemListItemDto> items = rows.Select(WarehouseItemListItemDto.FromRow).ToList();

        return Result<IReadOnlyList<WarehouseItemListItemDto>>.Success(items);
    }
}
