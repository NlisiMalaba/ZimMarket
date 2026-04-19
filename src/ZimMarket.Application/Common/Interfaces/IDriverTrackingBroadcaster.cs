namespace ZimMarket.Application.Common.Interfaces;

public interface IDriverTrackingBroadcaster
{
    Task BroadcastDriverLocationUpdatedAsync(
        Guid driverId,
        double latitude,
        double longitude,
        IReadOnlyList<Guid> activeOrderIds,
        CancellationToken cancellationToken = default);
}
