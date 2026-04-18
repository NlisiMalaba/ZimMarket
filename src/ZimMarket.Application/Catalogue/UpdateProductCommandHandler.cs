using MediatR;
using Microsoft.Extensions.Logging;
using ZimMarket.Application.Auth;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Application.Common.Models;
using ZimMarket.Domain.Entities.Catalogue;
using ZimMarket.Domain.Enums;
using ZimMarket.Domain.Exceptions;
using ZimMarket.Domain.Interfaces;
using ZimMarket.Domain.ValueObjects;

namespace ZimMarket.Application.Catalogue;

public sealed class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand, Result>
{
    private readonly ICurrentUser _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileStorage _fileStorage;
    private readonly ICacheService _cacheService;
    private readonly ILogger<UpdateProductCommandHandler> _logger;

    public UpdateProductCommandHandler(
        ICurrentUser currentUser,
        IUnitOfWork unitOfWork,
        IFileStorage fileStorage,
        ICacheService cacheService,
        ILogger<UpdateProductCommandHandler> logger)
    {
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _fileStorage = fileStorage ?? throw new ArgumentNullException(nameof(fileStorage));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        if (!IsSellerKycApproved())
        {
            _logger.LogDebug("Update product rejected: caller is not a seller with approved KYC.");
            return Result.Failure("Products.Forbidden", "Only KYC-approved sellers can update products.");
        }

        Product? product = await _unitOfWork.Products.GetByIdAsync(request.ProductId, cancellationToken).ConfigureAwait(false);
        if (product is null)
            return Result.Failure("Products.NotFound", "Product was not found.");

        if (product.SellerId != _currentUser.UserId)
            return Result.Failure("Products.Forbidden", "You can only update your own products.");

        if (!await _unitOfWork.Categories.ExistsAsync(request.CategoryId, cancellationToken).ConfigureAwait(false))
        {
            return Result.ValidationFailure(
            [
                new ValidationError(nameof(UpdateProductCommand.CategoryId), "The selected category does not exist.")
            ]);
        }

        Result? imageValidation = await ValidateImageKeysAsync(request.ImageKeys, cancellationToken).ConfigureAwait(false);
        if (imageValidation is not null)
            return imageValidation;

        var priceResult = Money.Create(request.PriceUsd, Currency.USD);
        if (!priceResult.IsSuccess || priceResult.Value is null)
        {
            return Result.ValidationFailure(
            [
                new ValidationError(nameof(UpdateProductCommand.PriceUsd), priceResult.Errors.FirstOrDefault() ?? "Price is invalid.")
            ]);
        }

        var addressResult = Address.Create(
            request.PickupAddress.Street,
            request.PickupAddress.Suburb,
            request.PickupAddress.City,
            request.PickupAddress.Country);
        if (!addressResult.IsSuccess || addressResult.Value is null)
        {
            return Result.ValidationFailure(
            [
                new ValidationError(
                    nameof(UpdateProductCommand.PickupAddress),
                    addressResult.Errors.FirstOrDefault() ?? "Pickup address is invalid.")
            ]);
        }

        try
        {
            product.UpdateDetails(
                request.Title,
                request.Description,
                priceResult.Value,
                request.CategoryId,
                request.ImageKeys,
                addressResult.Value);
        }
        catch (DomainException ex)
        {
            return Result.ValidationFailure([new ValidationError(string.Empty, ex.Message)]);
        }

        await _unitOfWork.Products.UpdateAsync(product, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await _cacheService.RemoveAsync(GetProductCacheKey(product.Id), cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }

    private bool IsSellerKycApproved()
    {
        if (!_currentUser.IsAuthenticated || _currentUser.Role != UserRole.Seller || _currentUser.UserId == Guid.Empty)
            return false;

        string? kycStatus = _currentUser.GetClaim(AuthClaimTypes.KycStatus);
        return string.Equals(kycStatus, KycStatus.Approved.ToString(), StringComparison.Ordinal);
    }

    private async Task<Result?> ValidateImageKeysAsync(
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
                    return Result.ValidationFailure(
                    [
                        new ValidationError(
                            $"{nameof(UpdateProductCommand.ImageKeys)}[{index}]",
                            "Image file was not found in storage. Upload the file first.")
                    ]);
                }
            }
            catch (ArgumentException ex)
            {
                return Result.ValidationFailure(
                [
                    new ValidationError($"{nameof(UpdateProductCommand.ImageKeys)}[{index}]", ex.Message)
                ]);
            }
        }

        return null;
    }

    private static string GetProductCacheKey(Guid productId) => $"product:{productId:D}";
}
