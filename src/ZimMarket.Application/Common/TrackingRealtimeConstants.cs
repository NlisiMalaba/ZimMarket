namespace ZimMarket.Application.Common;

/// <summary>SignalR group names and client callbacks for delivery tracking.</summary>
public static class TrackingRealtimeConstants
{
    /// <summary>Client receives <c>LocationUpdated(driverId, latitude, longitude, timestampUtc)</c>.</summary>
    public const string LocationUpdatedMethod = "LocationUpdated";

    public static string OrderGroupName(Guid orderId) => $"order:{orderId:D}";

    public const string AdminDriversGroupName = "admin:drivers";
}
