using Microsoft.EntityFrameworkCore;
using ZimMarket.Domain.Entities.Catalogue;
using ZimMarket.Domain.Enums;
using ZimMarket.Domain.Interfaces.Repositories;
using ZimMarket.Domain.ReadModels;

namespace ZimMarket.Infrastructure.Persistence.Repositories;

internal sealed class SellerDashboardReadRepository : ISellerDashboardReadRepository
{
    private readonly AppDbContext _dbContext;

    public SellerDashboardReadRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<SellerDashboardStatsRaw> GetAsync(
        Guid sellerId,
        int lowStockBelowQuantity,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Guid> sellerProductIds = _dbContext.Products.AsNoTracking()
            .Where(p => p.SellerId == sellerId)
            .Select(p => p.Id);

        IQueryable<Domain.Entities.Orders.Order> sellerOrders = _dbContext.Orders.AsNoTracking()
            .Where(o => o.Items.Any(i => sellerProductIds.Contains(i.ProductId)));

        int totalOrders = await sellerOrders.CountAsync(cancellationToken).ConfigureAwait(false);

        decimal totalRevenueUsd = await sellerOrders
            .Where(o => o.PaymentStatus == PaymentStatus.Paid)
            .SumAsync(o => o.TotalAmount.Amount, cancellationToken)
            .ConfigureAwait(false);

        int activeListings = await _dbContext.Products.AsNoTracking()
            .CountAsync(
                p => p.SellerId == sellerId && p.Status == ProductStatus.Active,
                cancellationToken)
            .ConfigureAwait(false);

        int lowStock = await _dbContext.Products.AsNoTracking()
            .CountAsync(
                p =>
                    p.SellerId == sellerId
                    && p.Status == ProductStatus.Active
                    && p.StockQuantity < lowStockBelowQuantity,
                cancellationToken)
            .ConfigureAwait(false);

        return new SellerDashboardStatsRaw(totalOrders, totalRevenueUsd, activeListings, lowStock);
    }
}
