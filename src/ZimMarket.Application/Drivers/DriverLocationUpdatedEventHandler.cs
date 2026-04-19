using MediatR;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Domain.Events;

namespace ZimMarket.Application.Drivers;

public sealed class DriverLocationUpdatedEventHandler : INotificationHandler<DriverLocationUpdatedEvent>
{
    private readonly IDriverTrackingBroadcaster _broadcaster;

    public DriverLocationUpdatedEventHandler(IDriverTrackingBroadcaster broadcaster)
    {
        _broadcaster = broadcaster ?? throw new ArgumentNullException(nameof(broadcaster));
    }

    public Task Handle(DriverLocationUpdatedEvent notification, CancellationToken cancellationToken) =>
        _broadcaster.BroadcastDriverLocationUpdatedAsync(
            notification.DriverId,
            notification.Lat,
            notification.Lng,
            notification.ActiveOrderIds,
            cancellationToken);
}
