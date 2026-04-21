using Microsoft.EntityFrameworkCore;
using ZimMarket.Domain.Entities.Orders;
using ZimMarket.Domain.Enums;
using ZimMarket.Domain.Interfaces.Repositories;
using ZimMarket.Domain.ReadModels;
using ZimMarket.Shared;

namespace ZimMarket.Infrastructure.Persistence.Repositories;

internal sealed class OrderRepository : IOrderRepository
{
    private readonly AppDbContext _dbContext;

    public OrderRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.Orders
            .AsNoTracking()
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<Order?> GetByIdWithItemsAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.Orders
            .AsNoTracking()
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<Order?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.Orders
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Order>> GetByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default) =>
        await _dbContext.Orders
            .AsNoTracking()
            .Where(x => x.CustomerId == customerId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<PagedList<Order>> GetByCustomerPagedAsync(
        Guid customerId,
        PaginationParams pagination,
        OrderStatus? statusFilter,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pagination);

        IQueryable<Order> query = _dbContext.Orders
            .AsNoTracking()
            .Where(x => x.CustomerId == customerId);

        if (statusFilter.HasValue)
            query = query.Where(x => x.Status == statusFilter.Value);

        query = query.OrderByDescending(x => x.CreatedAt);

        long totalCount = await query.LongCountAsync(cancellationToken).ConfigureAwait(false);
        List<Order> items = await query
            .Skip((pagination.Page - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedList<Order>(items, pagination.Page, pagination.PageSize, totalCount);
    }

    public async Task<PagedList<Order>> GetBySellerPagedAsync(
        Guid sellerId,
        PaginationParams pagination,
        OrderStatus? statusFilter,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pagination);

        IQueryable<Guid> sellerProductIds = _dbContext.Products
            .AsNoTracking()
            .Where(p => p.SellerId == sellerId)
            .Select(p => p.Id);

        IQueryable<Order> query = _dbContext.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .Where(o => o.Items.Any(i => sellerProductIds.Contains(i.ProductId)));

        if (statusFilter.HasValue)
            query = query.Where(o => o.Status == statusFilter.Value);

        query = query.OrderByDescending(o => o.CreatedAt);

        long totalCount = await query.LongCountAsync(cancellationToken).ConfigureAwait(false);
        List<Order> items = await query
            .Skip((pagination.Page - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedList<Order>(items, pagination.Page, pagination.PageSize, totalCount);
    }

    public async Task<PagedList<OrderListAdminRow>> GetAllPagedForAdminAsync(
        OrderStatus? status,
        DateTimeOffset? dateFromInclusive,
        DateTimeOffset? dateToInclusive,
        PaginationParams pagination,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pagination);

        IQueryable<Order> query = _dbContext.Orders.AsNoTracking();

        if (status.HasValue)
            query = query.Where(o => o.Status == status.Value);

        if (dateFromInclusive.HasValue)
            query = query.Where(o => o.CreatedAt >= dateFromInclusive.Value);

        if (dateToInclusive.HasValue)
            query = query.Where(o => o.CreatedAt <= dateToInclusive.Value);

        query = query.OrderByDescending(o => o.CreatedAt);

        long totalCount = await query.LongCountAsync(cancellationToken).ConfigureAwait(false);

        List<OrderListAdminRow> items = await query
            .Skip((pagination.Page - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .Select(o => new OrderListAdminRow(
                o.Id,
                o.CustomerId,
                o.Status,
                o.PaymentStatus,
                o.TotalAmount.Amount,
                o.TotalAmount.Currency,
                o.Items.Count,
                o.CreatedAt))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedList<OrderListAdminRow>(items, pagination.Page, pagination.PageSize, totalCount);
    }

    public async Task<IReadOnlyList<Order>> GetByStatusAsync(OrderStatus status, CancellationToken cancellationToken = default) =>
        await _dbContext.Orders
            .AsNoTracking()
            .Where(x => x.Status == status)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Order order, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(order);
        await _dbContext.Orders.AddAsync(order, cancellationToken);
    }

    public Task UpdateAsync(Order order, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(order);
        _dbContext.Orders.Update(order);
        return Task.CompletedTask;
    }
}
