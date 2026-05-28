using MediatR;
using Microsoft.Extensions.Logging;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Application.Common.Models;
using ZimMarket.Application.Files;
using ZimMarket.Domain.Entities.Users;
using ZimMarket.Domain.Enums;
using ZimMarket.Domain.Exceptions;
using ZimMarket.Domain.Interfaces;
using ZimMarket.Domain.Interfaces.Repositories;
using ZimMarket.Domain.ValueObjects;

namespace ZimMarket.Application.Sellers;

public sealed class UpdateSellerProfileCommandHandler : IRequestHandler<UpdateSellerProfileCommand, Result>
{
    private readonly ICurrentUser _currentUser;
    private readonly IUserLoginRepository _userLoginRepository;
    private readonly IUserIdentityReadRepository _userIdentityRead;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileStorage _fileStorage;
    private readonly ILogger<UpdateSellerProfileCommandHandler> _logger;

    public UpdateSellerProfileCommandHandler(
        ICurrentUser currentUser,
        IUserLoginRepository userLoginRepository,
        IUserIdentityReadRepository userIdentityRead,
        IUnitOfWork unitOfWork,
        IFileStorage fileStorage,
        ILogger<UpdateSellerProfileCommandHandler> logger)
    {
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _userLoginRepository = userLoginRepository ?? throw new ArgumentNullException(nameof(userLoginRepository));
        _userIdentityRead = userIdentityRead ?? throw new ArgumentNullException(nameof(userIdentityRead));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _fileStorage = fileStorage ?? throw new ArgumentNullException(nameof(fileStorage));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result> Handle(UpdateSellerProfileCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.Role != UserRole.Seller)
        {
            return Result.Failure("Seller.Forbidden", "Only authenticated sellers can update their profile.");
        }

        string normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var phoneResult = PhoneNumber.Create(request.Phone.Trim());
        if (phoneResult.IsFailure)
        {
            return Result.ValidationFailure(
            [
                new ValidationError(
                    nameof(UpdateSellerProfileCommand.Phone),
                    string.Join("; ", phoneResult.Errors))
            ]);
        }

        PhoneNumber phone = phoneResult.Value!;

        if (await _userIdentityRead
                .ExistsWithEmailForOtherUserAsync(normalizedEmail, _currentUser.UserId, cancellationToken)
                .ConfigureAwait(false))
        {
            return Result.Failure("Seller.EmailAlreadyExists", "This email is already registered.");
        }

        if (await _userIdentityRead
                .ExistsWithPhoneForOtherUserAsync(phone, _currentUser.UserId, cancellationToken)
                .ConfigureAwait(false))
        {
            return Result.Failure("Seller.PhoneAlreadyExists", "This phone number is already registered.");
        }

        User? trackedUser = await _userLoginRepository
            .GetTrackedByIdAsync(_currentUser.UserId, cancellationToken)
            .ConfigureAwait(false);

        if (trackedUser is not Seller seller)
        {
            return Result.Failure("Seller.NotFound", "Seller profile was not found.");
        }

        string? profilePhotoKey = string.IsNullOrWhiteSpace(request.ProfilePhotoKey)
            ? null
            : request.ProfilePhotoKey.Trim();

        if (profilePhotoKey is not null &&
            !ProfilePhotoStorage.IsProfilePhotoKeyForUser(profilePhotoKey, seller.Id))
        {
            return Result.ValidationFailure(
            [
                new ValidationError(
                    nameof(UpdateSellerProfileCommand.ProfilePhotoKey),
                    "Profile photo key is invalid for this account.")
            ]);
        }

        Address? defaultPickupAddress = null;
        if (!request.ClearDefaultPickupAddress && request.DefaultPickupAddress is not null)
        {
            var addressResult = Address.Create(
                request.DefaultPickupAddress.Street,
                request.DefaultPickupAddress.Suburb,
                request.DefaultPickupAddress.City,
                request.DefaultPickupAddress.Country);

            if (!addressResult.IsSuccess || addressResult.Value is null)
            {
                return Result.ValidationFailure(
                [
                    new ValidationError(
                        nameof(UpdateSellerProfileCommand.DefaultPickupAddress),
                        addressResult.Errors.FirstOrDefault() ?? "Default pickup address is invalid.")
                ]);
            }

            defaultPickupAddress = addressResult.Value;
        }

        string? previousProfilePhotoKey = seller.ProfilePhotoKey;

        try
        {
            seller.UpdateFullName(request.FullName);
            seller.UpdateEmail(normalizedEmail);
            seller.UpdatePhoneNumber(phone);
            seller.UpdateBusinessName(request.BusinessName);
            seller.SetProfilePhotoKey(profilePhotoKey);
            seller.SetDefaultPickupAddress(defaultPickupAddress);
        }
        catch (DomainException ex)
        {
            return Result.ValidationFailure([new ValidationError(string.Empty, ex.Message)]);
        }
        catch (ArgumentException ex)
        {
            return Result.ValidationFailure([new ValidationError(string.Empty, ex.Message)]);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        if (!string.IsNullOrWhiteSpace(previousProfilePhotoKey) &&
            !string.Equals(previousProfilePhotoKey, profilePhotoKey, StringComparison.Ordinal))
        {
            await ProfilePhotoStorage
                .TryDeleteAsync(_fileStorage, _logger, seller.Id, previousProfilePhotoKey, cancellationToken)
                .ConfigureAwait(false);
        }

        return Result.Success();
    }
}
