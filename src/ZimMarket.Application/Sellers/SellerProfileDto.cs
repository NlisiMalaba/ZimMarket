using ZimMarket.Application.Catalogue;

namespace ZimMarket.Application.Sellers;

public sealed class SellerProfileDto
{
    public required string FullName { get; init; }

    public required string Email { get; init; }

    public required string Phone { get; init; }

    public required string BusinessName { get; init; }

    public string? ProfilePhotoKey { get; init; }

    public string? ProfilePhotoUrl { get; init; }

    public PickupAddressDto? DefaultPickupAddress { get; init; }
}
