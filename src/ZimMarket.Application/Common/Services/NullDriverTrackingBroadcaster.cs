using ZimMarket.Application.Common.Interfaces;

namespace ZimMarket.Application.Common.Services;

public sealed class NullDriverTrackingBroadcaster : IDriverTrackingBroadcaster
{
    public Task BroadcastDriverLocationUpdatedAsync(
        Guid driverId,
        double latitude,
        double longitude,
        IReadOnlyList<Guid> activeOrderIds,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
