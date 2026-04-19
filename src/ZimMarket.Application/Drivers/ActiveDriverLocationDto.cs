namespace ZimMarket.Application.Drivers;

public sealed record ActiveDriverLocationDto(
    Guid DriverId,
    double? Latitude,
    double? Longitude,
    DateTimeOffset? UpdatedAtUtc);
