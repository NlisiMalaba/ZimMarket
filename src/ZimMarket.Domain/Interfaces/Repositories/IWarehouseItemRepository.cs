using ZimMarket.Domain.Entities.Warehouse;
using ZimMarket.Domain.Enums;
using ZimMarket.Domain.ReadModels;
using ZimMarket.Shared;

namespace ZimMarket.Domain.Interfaces.Repositories;

public interface IWarehouseItemRepository
{
    Task<WarehouseItem?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WarehouseItem>> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WarehouseItem>> GetByOrderIdForUpdateAsync(Guid orderId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WarehouseItem>> GetUnbatchedAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WarehouseItemListRow>> GetUnbatchedWithOrderAsync(CancellationToken cancellationToken = default);

    Task<PagedList<WarehouseItemListRow>> GetPagedForAdminAsync(
        WarehouseQcStatus? qcStatusFilter,
        PaginationParams pagination,
        CancellationToken cancellationToken = default);

    Task AddAsync(WarehouseItem item, CancellationToken cancellationToken = default);

    Task UpdateAsync(WarehouseItem item, CancellationToken cancellationToken = default);
}
