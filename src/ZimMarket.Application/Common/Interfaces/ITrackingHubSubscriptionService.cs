using ZimMarket.Domain.Enums;

namespace ZimMarket.Application.Common.Interfaces;

/// <summary>Authorizes SignalR tracking hub group subscriptions (read-only order access).</summary>
public interface ITrackingHubSubscriptionService
{
    Task<bool> CanCustomerTrackOrderAsync(
        Guid customerUserId,
        Guid orderId,
        CancellationToken cancellationToken = default);

    bool CanAdminTrackDriverMap(UserRole role);
}
