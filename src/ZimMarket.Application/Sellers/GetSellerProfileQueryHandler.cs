using MediatR;
using ZimMarket.Application.Catalogue;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Application.Common.Models;
using ZimMarket.Domain.Entities.Users;
using ZimMarket.Domain.Enums;
using ZimMarket.Domain.Interfaces;
using ZimMarket.Domain.ValueObjects;

namespace ZimMarket.Application.Sellers;

public sealed class GetSellerProfileQueryHandler
    : IRequestHandler<GetSellerProfileQuery, Result<SellerProfileDto>>
{
    private static readonly TimeSpan ProfilePhotoReadTtl = TimeSpan.FromHours(24);

    private readonly ICurrentUser _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileStorage _fileStorage;

    public GetSellerProfileQueryHandler(
        ICurrentUser currentUser,
        IUnitOfWork unitOfWork,
        IFileStorage fileStorage)
    {
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _fileStorage = fileStorage ?? throw new ArgumentNullException(nameof(fileStorage));
    }

    public async Task<Result<SellerProfileDto>> Handle(
        GetSellerProfileQuery request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.Role != UserRole.Seller)
        {
            return Result<SellerProfileDto>.Failure(
                "Seller.Forbidden",
                "Only authenticated sellers can view their profile.");
        }

        Seller? seller = await _unitOfWork.Sellers
            .GetByIdAsync(_currentUser.UserId, cancellationToken)
            .ConfigureAwait(false);

        if (seller is null)
        {
            return Result<SellerProfileDto>.Failure("Seller.NotFound", "Seller profile was not found.");
        }

        string? profilePhotoUrl = null;
        if (!string.IsNullOrWhiteSpace(seller.ProfilePhotoKey))
        {
            DateTimeOffset expiresAt = DateTimeOffset.UtcNow.Add(ProfilePhotoReadTtl);
            profilePhotoUrl = await _fileStorage
                .GenerateSasUrlAsync(seller.ProfilePhotoKey.Trim(), expiresAt, cancellationToken)
                .ConfigureAwait(false);
        }

        return Result<SellerProfileDto>.Success(MapToDto(seller, profilePhotoUrl));
    }

    private static SellerProfileDto MapToDto(Seller seller, string? profilePhotoUrl) =>
        new()
        {
            FullName = seller.FullName,
            Email = seller.Email,
            Phone = seller.PhoneNumber.Value,
            BusinessName = seller.BusinessName,
            ProfilePhotoKey = seller.ProfilePhotoKey,
            ProfilePhotoUrl = profilePhotoUrl,
            DefaultPickupAddress = MapAddress(seller.DefaultPickupAddress),
        };

    private static PickupAddressDto? MapAddress(Address? address) =>
        address is null
            ? null
            : new PickupAddressDto(address.Street, address.Suburb, address.City, address.Country);
}
