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
        Task<int> ordersTodayTask = _dbContext.Orders.AsNoTracking()
            .Where(o => o.CreatedAt >= utcDayStart && o.CreatedAt < utcDayEndExclusive)
            .CountAsync(cancellationToken);

        Task<List<PaidOrderTotalRow>> paidTotalsTask = _dbContext.Orders.AsNoTracking()
            .Where(o =>
                o.CreatedAt >= utcDayStart
                && o.CreatedAt < utcDayEndExclusive
                && o.PaymentStatus == PaymentStatus.Paid)
            .Select(o => new PaidOrderTotalRow(o.TotalAmount.Amount, o.TotalAmount.Currency))
            .ToListAsync(cancellationToken);

        Task<long> pendingSellersTask = _dbContext.Sellers.AsNoTracking()
            .LongCountAsync(s => s.KycStatus == KycStatus.PendingReview, cancellationToken);

        Task<long> pendingDriversTask = _dbContext.Drivers.AsNoTracking()
            .LongCountAsync(d => d.KycStatus == KycStatus.PendingReview, cancellationToken);

        Task<int> activeDriversTask = _dbContext.Drivers.AsNoTracking()
            .Where(d =>
                d.IsActive
                && d.IsApproved
                && (d.DriverStatus == DriverStatus.Available || d.DriverStatus == DriverStatus.OnDelivery))
            .CountAsync(cancellationToken);

        Task<int> lowStockTask = _dbContext.Products.AsNoTracking()
            .Where(p => p.Status == ProductStatus.Active && p.StockQuantity <= lowStockMaxQuantityInclusive)
            .CountAsync(cancellationToken);

        await Task.WhenAll(
                ordersTodayTask,
                paidTotalsTask,
                pendingSellersTask,
                pendingDriversTask,
                activeDriversTask,
                lowStockTask)
            .ConfigureAwait(false);

        int ordersToday = await ordersTodayTask.ConfigureAwait(false);
        List<PaidOrderTotalRow> paidTotals = await paidTotalsTask.ConfigureAwait(false);
        long pendingSellers = await pendingSellersTask.ConfigureAwait(false);
        long pendingDrivers = await pendingDriversTask.ConfigureAwait(false);
        int activeDrivers = await activeDriversTask.ConfigureAwait(false);
        int lowStock = await lowStockTask.ConfigureAwait(false);

        int pendingKyc = (int)(pendingSellers + pendingDrivers);
        return new DashboardStatsRaw(ordersToday, paidTotals, pendingKyc, activeDrivers, lowStock);
    }
}
