using Microsoft.EntityFrameworkCore;
using ZimMarket.Domain.Entities.Catalogue;
using ZimMarket.Domain.Entities.Orders;
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
    public async Task<DashboardStatsRaw> GetOperationalAsync(
        DateTimeOffset utcDayStart,
        DateTimeOffset utcDayEndExclusive,
        int lowStockMaxQuantityInclusive,
        CancellationToken cancellationToken = default)
    {
        int ordersToday = await _dbContext.Orders.AsNoTracking()
            .Where(o => o.CreatedAt >= utcDayStart && o.CreatedAt < utcDayEndExclusive)
            .CountAsync(cancellationToken)
            .ConfigureAwait(false);

        int pendingSellers = await _dbContext.Sellers.AsNoTracking()
            .CountAsync(s => s.KycStatus == KycStatus.PendingReview, cancellationToken)
            .ConfigureAwait(false);

        int pendingDrivers = await _dbContext.Drivers.AsNoTracking()
            .CountAsync(d => d.KycStatus == KycStatus.PendingReview, cancellationToken)
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

        return new DashboardStatsRaw(ordersToday, pendingSellers, pendingDrivers, activeDrivers, lowStock);
    }

    /// <inheritdoc />
    public async Task<FinanceDashboardStatsRaw> GetFinanceAsync(
        DateTimeOffset utcDayStart,
        DateTimeOffset utcDayEndExclusive,
        DateTimeOffset utcMonthStart,
        DateTimeOffset utcYearStart,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<PaidOrderTotalRow> today = await GetPaidOrderTotalsAsync(
                utcDayStart,
                utcDayEndExclusive,
                cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<PaidOrderTotalRow> month = await GetPaidOrderTotalsAsync(
                utcMonthStart,
                utcDayEndExclusive,
                cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<PaidOrderTotalRow> year = await GetPaidOrderTotalsAsync(
                utcYearStart,
                utcDayEndExclusive,
                cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<PaidOrderTotalRow> allTime = await GetPaidOrderTotalsAsync(
                null,
                null,
                cancellationToken)
            .ConfigureAwait(false);

        return new FinanceDashboardStatsRaw(today, month, year, allTime);
    }

    private async Task<List<PaidOrderTotalRow>> GetPaidOrderTotalsAsync(
        DateTimeOffset? createdFromInclusive,
        DateTimeOffset? createdBeforeExclusive,
        CancellationToken cancellationToken)
    {
        IQueryable<Order> query = _dbContext.Orders.AsNoTracking()
            .Where(o => o.PaymentStatus == PaymentStatus.Paid);

        if (createdFromInclusive is not null)
        {
            query = query.Where(o => o.CreatedAt >= createdFromInclusive);
        }

        if (createdBeforeExclusive is not null)
        {
            query = query.Where(o => o.CreatedAt < createdBeforeExclusive);
        }

        return await query
            .Select(o => new PaidOrderTotalRow(o.TotalAmount.Amount, o.TotalAmount.Currency))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
