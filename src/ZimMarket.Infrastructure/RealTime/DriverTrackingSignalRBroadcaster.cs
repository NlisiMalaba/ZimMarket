using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using ZimMarket.Application.Common.Interfaces;

namespace ZimMarket.Infrastructure.RealTime;

public sealed class DriverTrackingSignalRBroadcaster : IDriverTrackingBroadcaster
{
    public const string DriverLocationUpdatedMethod = "driverLocationUpdated";

    private readonly IHubContext<TrackingHub> _hubContext;
    private readonly ILogger<DriverTrackingSignalRBroadcaster> _logger;

    public DriverTrackingSignalRBroadcaster(
        IHubContext<TrackingHub> hubContext,
        ILogger<DriverTrackingSignalRBroadcaster> logger)
    {
        _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task BroadcastDriverLocationUpdatedAsync(
        Guid driverId,
        double latitude,
        double longitude,
        IReadOnlyList<Guid> activeOrderIds,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = new
            {
                driverId,
                latitude,
                longitude,
                activeOrderIds
            };

            foreach (Guid orderId in activeOrderIds)
            {
                await _hubContext.Clients
                    .Group($"order:{orderId:D}")
                    .SendAsync(DriverLocationUpdatedMethod, payload, cancellationToken)
                    .ConfigureAwait(false);
            }

            await _hubContext.Clients
                .Group("admin:drivers")
                .SendAsync(DriverLocationUpdatedMethod, payload, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to broadcast driver location for driver {DriverId}.",
                driverId);
        }
    }
}
