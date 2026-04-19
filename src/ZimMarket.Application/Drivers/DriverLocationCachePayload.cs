namespace ZimMarket.Application.Drivers;

public sealed record DriverLocationCachePayload(double Latitude, double Longitude, DateTimeOffset UpdatedAtUtc);
