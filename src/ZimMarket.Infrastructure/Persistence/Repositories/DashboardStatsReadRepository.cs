using Microsoft.EntityFrameworkCore;
using ZimMarket.Domain.Entities.Catalogue;
using ZimMarket.Domain.Enums;
using ZimMarket.Domain.Interfaces.Repositories;
using ZimMarket.Domain.ReadModels;

namespace ZimMarket.Infrastructure.Persistence.Repositories;

internal sealed class DashboardStatsReadRepository : IDashboardStatsReadRepository
{
    private readonly AppDbContext _dbContext;

    public DashboardStatsReadRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    /// <inheritdoc />
    public async Task<DashboardStatsRaw> GetAsync(
        DateTimeOffset utcDayStart,
        DateTimeOffset utcDayEndExclusive,
        int lowStockMaxQuantityInclusive,
        CancellationToken cancellationToken = default)
    {
        int ordersToday = await _dbContext.Orders.AsNoTracking()
            .Where(o => o.CreatedAt >= utcDayStart && o.CreatedAt < utcDayEndExclusive)
            .CountAsync(cancellationToken)
            .ConfigureAwait(false);

        List<PaidOrderTotalRow> paidTotals = await _dbContext.Orders.AsNoTracking()
            .Where(o =>
                o.CreatedAt >= utcDayStart
                && o.CreatedAt < utcDayEndExclusive
                && o.PaymentStatus == PaymentStatus.Paid)
            .Select(o => new PaidOrderTotalRow(o.TotalAmount.Amount, o.TotalAmount.Currency))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        long pendingSellers = await _dbContext.Sellers.AsNoTracking()
            .LongCountAsync(s => s.KycStatus == KycStatus.PendingReview, cancellationToken)
            .ConfigureAwait(false);

        long pendingDrivers = await _dbContext.Drivers.AsNoTracking()
            .LongCountAsync(d => d.KycStatus == KycStatus.PendingReview, cancellationToken)
            .ConfigureAwait(false);

        int activeDrivers = await _dbContext.Drivers.AsNoTracking()
            .Where(d =>
                d.IsActive
                && d.IsApproved
                && (d.DriverStatus == DriverStatus.Available || d.DriverStatus == DriverStatus.OnDelivery))
            .CountAsync(cancellationToken)
            .ConfigureAwait(false);

        int lowStock = await _dbContext.Products.AsNoTracking()
            .Where(p => p.Status == ProductStatus.Active && p.StockQuantity <= lowStockMaxQuantityInclusive)
            .CountAsync(cancellationToken)
            .ConfigureAwait(false);

        int pendingKyc = (int)(pendingSellers + pendingDrivers);
        return new DashboardStatsRaw(ordersToday, paidTotals, pendingKyc, activeDrivers, lowStock);
    }
}
