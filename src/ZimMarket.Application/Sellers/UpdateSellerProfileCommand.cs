using ZimMarket.Application.Catalogue;
using ZimMarket.Application.Common.Abstractions;
using ZimMarket.Application.Common.Models;

namespace ZimMarket.Application.Sellers;

public sealed record UpdateSellerProfileCommand(
    string FullName,
    string Email,
    string Phone,
    string BusinessName,
    string? ProfilePhotoKey,
    PickupAddressDto? DefaultPickupAddress,
    bool ClearDefaultPickupAddress) : ICommand;
