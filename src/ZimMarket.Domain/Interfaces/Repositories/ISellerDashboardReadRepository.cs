using ZimMarket.Domain.ReadModels;

namespace ZimMarket.Domain.Interfaces.Repositories;

public interface ISellerDashboardReadRepository
{
    Task<SellerDashboardStatsRaw> GetAsync(
        Guid sellerId,
        int lowStockBelowQuantity,
        CancellationToken cancellationToken = default);
}
