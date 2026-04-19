using ZimMarket.Domain.Enums;
using ZimMarket.Domain.ReadModels;
using ZimMarket.Shared;

namespace ZimMarket.Domain.Interfaces.Repositories;

public interface IPendingKycReadRepository
{
    /// <summary>
    /// Paginated users with <see cref="KycStatus.PendingReview"/> for the given <paramref name="role"/>
    /// (<see cref="UserRole.Seller"/> or <see cref="UserRole.Driver"/> only).
    /// </summary>
    Task<PagedList<PendingKycQueueRow>> GetPagedPendingReviewAsync(
        UserRole role,
        PaginationParams pagination,
        CancellationToken cancellationToken = default);
}
