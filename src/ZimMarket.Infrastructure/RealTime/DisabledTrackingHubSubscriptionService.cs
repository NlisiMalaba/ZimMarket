using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Domain.Enums;

namespace ZimMarket.Infrastructure.RealTime;

/// <summary>
/// Fallback tracking subscription service used when persistence services are not configured.
/// </summary>
public sealed class DisabledTrackingHubSubscriptionService : ITrackingHubSubscriptionService
{
    public Task<bool> CanCustomerTrackOrderAsync(
        Guid customerUserId,
        Guid orderId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(false);

    public bool CanAdminTrackDriverMap(UserRole role) => false;
}
