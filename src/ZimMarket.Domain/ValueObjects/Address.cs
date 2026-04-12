using ZimMarket.Shared;

namespace ZimMarket.Domain.ValueObjects;

public sealed class Address
{
    public const int MaxStreetLength = 200;
    public const int MaxSuburbLength = 100;
    public const int MaxCityLength = 100;
    public const int MaxCountryLength = 100;

    private Address(string street, string suburb, string city, string country)
    {
        Street = street;
        Suburb = suburb;
        City = city;
        Country = country;
    }

    public string Street { get; }

    public string Suburb { get; }

    public string City { get; }

    public string Country { get; }

    public static Result<Address> Create(string street, string suburb, string city, string country)
    {
        street = street.Trim();
        suburb = suburb.Trim();
        city = city.Trim();
        country = country.Trim();

        if (string.IsNullOrWhiteSpace(street))
            return Result<Address>.Failure("Street is required.");

        if (string.IsNullOrWhiteSpace(suburb))
            return Result<Address>.Failure("Suburb is required.");

        if (string.IsNullOrWhiteSpace(city))
            return Result<Address>.Failure("City is required.");

        if (string.IsNullOrWhiteSpace(country))
            return Result<Address>.Failure("Country is required.");

        if (street.Length > MaxStreetLength)
            return Result<Address>.Failure($"Street cannot exceed {MaxStreetLength} characters.");

        if (suburb.Length > MaxSuburbLength)
            return Result<Address>.Failure($"Suburb cannot exceed {MaxSuburbLength} characters.");

        if (city.Length > MaxCityLength)
            return Result<Address>.Failure($"City cannot exceed {MaxCityLength} characters.");

        if (country.Length > MaxCountryLength)
            return Result<Address>.Failure($"Country cannot exceed {MaxCountryLength} characters.");

        return Result<Address>.Success(new Address(street, suburb, city, country));
    }
}
