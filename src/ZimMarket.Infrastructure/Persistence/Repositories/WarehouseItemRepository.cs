using Microsoft.EntityFrameworkCore;
using ZimMarket.Domain.Entities.Warehouse;
using ZimMarket.Domain.Enums;
using ZimMarket.Domain.Interfaces.Repositories;
using ZimMarket.Domain.ReadModels;
using ZimMarket.Shared;

namespace ZimMarket.Infrastructure.Persistence.Repositories;

internal sealed class WarehouseItemRepository : IWarehouseItemRepository
{
    private readonly AppDbContext _dbContext;

    public WarehouseItemRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public Task<WarehouseItem?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.WarehouseItems.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<WarehouseItem>> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default) =>
        await _dbContext.WarehouseItems
            .AsNoTracking()
            .Where(x => x.OrderId == orderId)
            .OrderByDescending(x => x.ArrivedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<WarehouseItem>> GetByOrderIdForUpdateAsync(
        Guid orderId,
        CancellationToken cancellationToken = default) =>
        await _dbContext.WarehouseItems
            .Where(x => x.OrderId == orderId)
            .OrderByDescending(x => x.ArrivedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<WarehouseItem>> GetUnbatchedAsync(CancellationToken cancellationToken = default) =>
        await _dbContext.WarehouseItems
            .AsNoTracking()
            .Where(x => x.BatchId == null && x.QcStatus == WarehouseQcStatus.Passed)
            .OrderBy(x => x.ArrivedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<WarehouseItemListRow>> GetUnbatchedWithOrderAsync(
        CancellationToken cancellationToken = default)
    {
        IQueryable<WarehouseItemListRow> query =
            from w in _dbContext.WarehouseItems.AsNoTracking()
            where w.BatchId == null && w.QcStatus == WarehouseQcStatus.Passed
            join o in _dbContext.Orders.AsNoTracking() on w.OrderId equals o.Id
            orderby w.ArrivedAt
            select new WarehouseItemListRow(
                w.Id,
                w.OrderId,
                o.CustomerId,
                w.ProductId,
                w.ArrivedAt,
                w.QcStatus,
                w.QcNotes,
                w.BatchId,
                w.CreatedAt,
                o.Status,
                o.PaymentStatus,
                o.TotalAmount.Amount,
                o.TotalAmount.Currency,
                o.CreatedAt);

        return await query.ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<PagedList<WarehouseItemListRow>> GetPagedForAdminAsync(
        WarehouseQcStatus? qcStatusFilter,
        PaginationParams pagination,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pagination);

        IQueryable<WarehouseItem> items = _dbContext.WarehouseItems.AsNoTracking();
        if (qcStatusFilter.HasValue)
            items = items.Where(x => x.QcStatus == qcStatusFilter.Value);

        IQueryable<WarehouseItemListRow> query =
            from w in items
            join o in _dbContext.Orders.AsNoTracking() on w.OrderId equals o.Id
            select new WarehouseItemListRow(
                w.Id,
                w.OrderId,
                o.CustomerId,
                w.ProductId,
                w.ArrivedAt,
                w.QcStatus,
                w.QcNotes,
                w.BatchId,
                w.CreatedAt,
                o.Status,
                o.PaymentStatus,
                o.TotalAmount.Amount,
                o.TotalAmount.Currency,
                o.CreatedAt);

        IOrderedQueryable<WarehouseItemListRow> ordered = query.OrderByDescending(x => x.ArrivedAt);

        long totalCount = await ordered.LongCountAsync(cancellationToken).ConfigureAwait(false);
        List<WarehouseItemListRow> page = await ordered
            .Skip((pagination.Page - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedList<WarehouseItemListRow>(page, pagination.Page, pagination.PageSize, totalCount);
    }

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
