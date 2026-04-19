using ZimMarket.Domain.Entities;

namespace ZimMarket.Domain.Entities.Logistics;

/// <summary>Latest known coordinates for a driver (one row per driver id).
/// </summary>
public sealed class DriverLocation : BaseEntity
{
    private DriverLocation()
    {
    }

    public double Latitude { get; private set; }

    public double Longitude { get; private set; }

    public static DriverLocation Create(
        Guid driverId,
        double latitude,
        double longitude,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        return new DriverLocation
        {
            Id = driverId,
            Latitude = latitude,
            Longitude = longitude,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };
    }

    public void SetPosition(double latitude, double longitude)
    {
        Latitude = latitude;
        Longitude = longitude;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
