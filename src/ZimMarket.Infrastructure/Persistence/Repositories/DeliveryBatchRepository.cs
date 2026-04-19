using Microsoft.EntityFrameworkCore;
using ZimMarket.Domain.Entities.Logistics;
using ZimMarket.Domain.Enums;
using ZimMarket.Domain.Interfaces.Repositories;

namespace ZimMarket.Infrastructure.Persistence.Repositories;

internal sealed class DeliveryBatchRepository : IDeliveryBatchRepository
{
    private readonly AppDbContext _dbContext;

    public DeliveryBatchRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public Task<DeliveryBatch?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.DeliveryBatches
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<DeliveryBatch?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.DeliveryBatches.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<DeliveryBatch?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        if (orderId == Guid.Empty)
            return null;

        List<DeliveryBatch> batches = await _dbContext.DeliveryBatches
            .AsNoTracking()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return batches.FirstOrDefault(x => x.OrderIds.Contains(orderId));
    }

    public Task<DeliveryBatch?> GetActiveByDriverAsync(Guid driverId, CancellationToken cancellationToken = default) =>
        _dbContext.DeliveryBatches
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.DriverId == driverId && x.Status != DeliveryBatchStatus.Completed,
                cancellationToken);

    public async Task<IReadOnlyList<DeliveryBatch>> GetPendingBatchesAsync(CancellationToken cancellationToken = default) =>
        await _dbContext.DeliveryBatches
            .AsNoTracking()
            .Where(x => x.Status == DeliveryBatchStatus.Created)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(DeliveryBatch batch, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(batch);
        await _dbContext.DeliveryBatches.AddAsync(batch, cancellationToken);
    }

    public Task UpdateAsync(DeliveryBatch batch, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(batch);
        _dbContext.DeliveryBatches.Update(batch);
        return Task.CompletedTask;
    }
}
