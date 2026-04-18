using MediatR;
using Microsoft.Extensions.Logging;
using ZimMarket.Application.Auth;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Application.Common.Models;
using ZimMarket.Domain.Entities.Catalogue;
using ZimMarket.Domain.Enums;
using ZimMarket.Domain.Interfaces;
using ZimMarket.Domain.ValueObjects;

namespace ZimMarket.Application.Catalogue;

public sealed class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, Result<Guid>>
{
    private readonly ICurrentUser _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileStorage _fileStorage;
    private readonly ILogger<CreateProductCommandHandler> _logger;

    public CreateProductCommandHandler(
        ICurrentUser currentUser,
        IUnitOfWork unitOfWork,
        IFileStorage fileStorage,
        ILogger<CreateProductCommandHandler> logger)
    {
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _fileStorage = fileStorage ?? throw new ArgumentNullException(nameof(fileStorage));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<Guid>> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        if (!IsSellerKycApproved())
        {
            _logger.LogDebug("Create product rejected: caller is not a seller with approved KYC.");
            return Result<Guid>.Failure("Products.Forbidden", "Only KYC-approved sellers can create products.");
        }

        if (!await _unitOfWork.Categories.ExistsAsync(request.CategoryId, cancellationToken).ConfigureAwait(false))
        {
            return Result<Guid>.ValidationFailure(
            [
                new ValidationError(nameof(CreateProductCommand.CategoryId), "The selected category does not exist.")
            ]);
        }

        Result<Guid>? imageValidation = await ValidateImageKeysAsync(request.ImageKeys, cancellationToken).ConfigureAwait(false);
        if (imageValidation is not null)
            return imageValidation;

        var priceResult = Money.Create(request.PriceUsd, Currency.USD);
        if (!priceResult.IsSuccess || priceResult.Value is null)
        {
            return Result<Guid>.ValidationFailure(
            [
                new ValidationError(nameof(CreateProductCommand.PriceUsd), priceResult.Errors.FirstOrDefault() ?? "Price is invalid.")
            ]);
        }

        var addressResult = Address.Create(
            request.PickupAddress.Street,
            request.PickupAddress.Suburb,
            request.PickupAddress.City,
            request.PickupAddress.Country);
        if (!addressResult.IsSuccess || addressResult.Value is null)
        {
            return Result<Guid>.ValidationFailure(
            [
                new ValidationError(
                    nameof(CreateProductCommand.PickupAddress),
                    addressResult.Errors.FirstOrDefault() ?? "Pickup address is invalid.")
            ]);
        }

        Guid productId = Guid.NewGuid();
        DateTimeOffset now = DateTimeOffset.UtcNow;
        var productResult = Product.Create(
            productId,
            _currentUser.UserId,
            request.Title,
            request.Description,
            priceResult.Value,
            request.CategoryId,
            request.StockQuantity,
            request.ImageKeys,
            addressResult.Value,
            now,
            now);

        if (!productResult.IsSuccess || productResult.Value is null)
        {
            return Result<Guid>.ValidationFailure(
            [
                new ValidationError(string.Empty, productResult.Errors.FirstOrDefault() ?? "Product data is invalid.")
            ]);
        }

        await _unitOfWork.Products.AddAsync(productResult.Value, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<Guid>.Success(productId);
    }

    private bool IsSellerKycApproved()
    {
        if (!_currentUser.IsAuthenticated || _currentUser.Role != UserRole.Seller || _currentUser.UserId == Guid.Empty)
            return false;

        string? kycStatus = _currentUser.GetClaim(AuthClaimTypes.KycStatus);
        return string.Equals(kycStatus, KycStatus.Approved.ToString(), StringComparison.Ordinal);
    }

    private async Task<Result<Guid>?> ValidateImageKeysAsync(
        IReadOnlyList<string> imageKeys,
        CancellationToken cancellationToken)
    {
        for (int index = 0; index < imageKeys.Count; index++)
        {
            string key = imageKeys[index].Trim();
            try
            {
                if (!await _fileStorage.ExistsAsync(key, cancellationToken).ConfigureAwait(false))
                {
                    return Result<Guid>.ValidationFailure(
                    [
                        new ValidationError(
                            $"{nameof(CreateProductCommand.ImageKeys)}[{index}]",
                            "Image file was not found in storage. Upload the file first.")
                    ]);
                }
            }
            catch (ArgumentException ex)
            {
                return Result<Guid>.ValidationFailure(
                [
                    new ValidationError($"{nameof(CreateProductCommand.ImageKeys)}[{index}]", ex.Message)
                ]);
            }
        }

        return null;
    }
}
