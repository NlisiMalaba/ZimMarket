using Microsoft.EntityFrameworkCore;
using ZimMarket.Domain.Common;
using ZimMarket.Domain.Entities.Catalogue;
using ZimMarket.Domain.Interfaces.Repositories;
using ZimMarket.Shared;

namespace ZimMarket.Infrastructure.Persistence.Repositories;

internal sealed class ProductRepository : IProductRepository
{
    private readonly AppDbContext _dbContext;

    public ProductRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.Products.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<PagedList<Product>> GetPagedAsync(
        ProductFilter filter,
        PaginationParams pagination,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(pagination);

        IQueryable<Product> query = _dbContext.Products.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            string term = filter.SearchTerm.Trim();
            query = query.Where(x => x.Title.Contains(term) || x.Description.Contains(term));
        }

        if (filter.CategoryId.HasValue)
            query = query.Where(x => x.CategoryId == filter.CategoryId.Value);

        if (filter.MinPriceUsd.HasValue)
            query = query.Where(x => x.Price.Amount >= filter.MinPriceUsd.Value);

        if (filter.MaxPriceUsd.HasValue)
            query = query.Where(x => x.Price.Amount <= filter.MaxPriceUsd.Value);

        query = query.OrderByDescending(x => x.CreatedAt);

        long totalCount = await query.LongCountAsync(cancellationToken);
        List<Product> items = await query
            .Skip((pagination.Page - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedList<Product>(items, pagination.Page, pagination.PageSize, totalCount);
    }

    public async Task<IReadOnlyList<Product>> FindBySellerAsync(Guid sellerId, CancellationToken cancellationToken = default) =>
        await _dbContext.Products
            .AsNoTracking()
            .Where(x => x.SellerId == sellerId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Product product, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(product);
        await _dbContext.Products.AddAsync(product, cancellationToken);
    }

    public Task UpdateAsync(Product product, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(product);
        _dbContext.Products.Update(product);
        return Task.CompletedTask;
    }
}
