namespace ZimMarket.Application.Drivers;

public static class DriverLocationCache
{
    public static readonly TimeSpan Ttl = TimeSpan.FromSeconds(90);

    public static string Key(Guid driverId) => $"driver-location:{driverId:D}";
}
