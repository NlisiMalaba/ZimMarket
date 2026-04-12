using ZimMarket.Shared;

namespace ZimMarket.Domain.ValueObjects;

public sealed class GeoCoordinate
{
    private GeoCoordinate(double latitude, double longitude)
    {
        Latitude = latitude;
        Longitude = longitude;
    }

    public double Latitude { get; }

    public double Longitude { get; }

    public static Result<GeoCoordinate> Create(double latitude, double longitude)
    {
        if (latitude is < -90 or > 90)
            return Result<GeoCoordinate>.Failure("Latitude must be between -90 and 90 degrees.");

        if (longitude is < -180 or > 180)
            return Result<GeoCoordinate>.Failure("Longitude must be between -180 and 180 degrees.");

        return Result<GeoCoordinate>.Success(new GeoCoordinate(latitude, longitude));
    }
}
