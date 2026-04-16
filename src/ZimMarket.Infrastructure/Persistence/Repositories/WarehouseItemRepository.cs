using Microsoft.EntityFrameworkCore;
using ZimMarket.Domain.Entities.Warehouse;
using ZimMarket.Domain.Enums;
using ZimMarket.Domain.Interfaces.Repositories;

namespace ZimMarket.Infrastructure.Persistence.Repositories;

internal sealed class WarehouseItemRepository : IWarehouseItemRepository
{
    private readonly AppDbContext _dbContext;

    public WarehouseItemRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<IReadOnlyList<WarehouseItem>> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default) =>
        await _dbContext.WarehouseItems
            .AsNoTracking()
            .Where(x => x.OrderId == orderId)
            .OrderByDescending(x => x.ArrivedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<WarehouseItem>> GetUnbatchedAsync(CancellationToken cancellationToken = default) =>
        await _dbContext.WarehouseItems
            .AsNoTracking()
            .Where(x => x.BatchId == null && x.QcStatus == WarehouseQcStatus.Passed)
            .OrderBy(x => x.ArrivedAt)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(WarehouseItem item, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);
        await _dbContext.WarehouseItems.AddAsync(item, cancellationToken);
    }

    public Task UpdateAsync(WarehouseItem item, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);
        _dbContext.WarehouseItems.Update(item);
        return Task.CompletedTask;
    }
}
