using ZimMarket.Domain.Entities.Logistics;
using ZimMarket.Domain.Enums;
using ZimMarket.Shared;

namespace ZimMarket.Domain.Interfaces.Repositories;

public interface IDeliveryBatchRepository
{
    Task<DeliveryBatch?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<DeliveryBatch?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default);

    Task<DeliveryBatch?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);

    Task<DeliveryBatch?> GetActiveByDriverAsync(Guid driverId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DeliveryBatch>> GetPendingBatchesAsync(CancellationToken cancellationToken = default);

    Task<PagedList<DeliveryBatch>> GetPagedAsync(
        DeliveryBatchStatus? statusFilter,
        PaginationParams pagination,
        CancellationToken cancellationToken = default);

    Task AddAsync(DeliveryBatch batch, CancellationToken cancellationToken = default);

    Task UpdateAsync(DeliveryBatch batch, CancellationToken cancellationToken = default);
}
