using Microsoft.EntityFrameworkCore;
using ZimMarket.Domain.Common;
using ZimMarket.Domain.Common.Specifications;
using ZimMarket.Domain.Entities.Catalogue;
using ZimMarket.Domain.Interfaces.Repositories;
using ZimMarket.Shared;
using ZimMarket.Infrastructure.Persistence.Specifications.Products;

namespace ZimMarket.Infrastructure.Persistence.Repositories;

internal sealed class ProductRepository : IProductRepository
{
    private readonly AppDbContext _dbContext;

    public ProductRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<PagedList<Product>> GetPagedAsync(
        ProductFilter filter,
        PaginationParams pagination,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(pagination);

        IQueryable<Product> query = _dbContext.Products.AsNoTracking();
        ISpecification<Product> specification = BuildSpecification(filter);
        query = ApplySpecification(query, specification);

        query = query.OrderByDescending(x => x.CreatedAt);

        long totalCount = await query.LongCountAsync(cancellationToken);
        List<Product> items = await query
            .Skip((pagination.Page - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedList<Product>(items, pagination.Page, pagination.PageSize, totalCount);
    }

    private static ISpecification<Product> BuildSpecification(ProductFilter filter)
    {
        var specifications = new List<ISpecification<Product>>();

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            specifications.Add(new SearchTermProductSpecification(filter.SearchTerm));

        if (filter.CategoryId.HasValue)
            specifications.Add(new CategoryProductSpecification(filter.CategoryId.Value));

        if (filter.MinPriceUsd.HasValue)
            specifications.Add(new MinPriceProductSpecification(filter.MinPriceUsd.Value));

        if (filter.MaxPriceUsd.HasValue)
            specifications.Add(new MaxPriceProductSpecification(filter.MaxPriceUsd.Value));

        if (filter.SellerId.HasValue)
            specifications.Add(new SellerProductSpecification(filter.SellerId.Value));

        return new CompositeSpecification<Product>(specifications);
    }

    private static IQueryable<Product> ApplySpecification(
        IQueryable<Product> query,
        ISpecification<Product> specification)
    {
        foreach (var criteria in specification.Criteria)
            query = query.Where(criteria);

        return query;
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
