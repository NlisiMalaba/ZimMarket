using ZimMarket.Domain.Entities.Warehouse;

namespace ZimMarket.Domain.Interfaces.Repositories;

public interface IWarehouseItemRepository
{
    Task<IReadOnlyList<WarehouseItem>> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WarehouseItem>> GetUnbatchedAsync(CancellationToken cancellationToken = default);

    Task AddAsync(WarehouseItem item, CancellationToken cancellationToken = default);

    Task UpdateAsync(WarehouseItem item, CancellationToken cancellationToken = default);
}
