using ZimMarket.Domain.Common;
using ZimMarket.Domain.Entities.Catalogue;
using ZimMarket.Domain.Enums;
using ZimMarket.Shared;

namespace ZimMarket.Domain.Interfaces.Repositories;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedList<Product>> GetPagedAsync(
        ProductFilter filter,
        PaginationParams pagination,
        CancellationToken cancellationToken = default);

    Task<PagedList<Product>> GetSellerPagedAsync(
        Guid sellerId,
        SellerProductListScope scope,
        PaginationParams pagination,
        CancellationToken cancellationToken = default);

    Task<Product?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken cancellationToken = default);

    Task HardDeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Product>> FindSoftDeletedOlderThanAsync(
        DateTimeOffset deletedBeforeUtc,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Product>> FindBySellerAsync(Guid sellerId, CancellationToken cancellationToken = default);

    Task AddAsync(Product product, CancellationToken cancellationToken = default);

    Task UpdateAsync(Product product, CancellationToken cancellationToken = default);
}
