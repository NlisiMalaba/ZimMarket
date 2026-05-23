using MediatR;
using ZimMarket.Application.Catalogue;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Application.Common.Models;
using ZimMarket.Domain.Enums;
using ZimMarket.Domain.Interfaces;

namespace ZimMarket.Application.Files;

public sealed class GetSellerProductImageQueryHandler
    : IRequestHandler<GetSellerProductImageQuery, Result<SellerProductImageContentDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly ILocalFileStorageAccess? _localFileStorageAccess;
    private readonly IFileStorage _fileStorage;

    public GetSellerProductImageQueryHandler(
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IFileStorage fileStorage,
        IEnumerable<ILocalFileStorageAccess> localFileStorageAccess)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _fileStorage = fileStorage ?? throw new ArgumentNullException(nameof(fileStorage));
        _localFileStorageAccess = localFileStorageAccess?.FirstOrDefault();
    }

    public async Task<Result<SellerProductImageContentDto>> Handle(
        GetSellerProductImageQuery request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId == Guid.Empty || _currentUser.Role != UserRole.Seller)
        {
            return Result<SellerProductImageContentDto>.Failure(
                "Files.Forbidden",
                "Only authenticated sellers can access product images.");
        }

        string imageKey = request.ImageKey.Trim();
        if (!ProductImageStorage.IsProductImageKey(imageKey))
        {
            return Result<SellerProductImageContentDto>.ValidationFailure(
            [
                new ValidationError(nameof(request.ImageKey), "Invalid product image key.")
            ]);
        }

        if (!await SellerOwnsImageKeyAsync(_currentUser.UserId, imageKey, cancellationToken).ConfigureAwait(false))
        {
            return Result<SellerProductImageContentDto>.Failure(
                "Files.Forbidden",
                "You do not have access to this product image.");
        }

        if (!await _fileStorage.ExistsAsync(imageKey, cancellationToken).ConfigureAwait(false))
        {
            return Result<SellerProductImageContentDto>.Failure(
                "Files.NotFound",
                "Product image file was not found in storage.");
        }

        if (_localFileStorageAccess is not null)
        {
            try
            {
                Stream stream = await _localFileStorageAccess
                    .OpenReadAsync(imageKey, cancellationToken)
                    .ConfigureAwait(false);

                string contentType = _localFileStorageAccess.GetContentType(imageKey);
                return Result<SellerProductImageContentDto>.Success(
                    new SellerProductImageContentDto(contentType, stream));
            }
            catch (FileNotFoundException)
            {
                return Result<SellerProductImageContentDto>.Failure(
                    "Files.NotFound",
                    "Product image file was not found in storage.");
            }
        }

        return Result<SellerProductImageContentDto>.Failure(
            "Files.StorageUnavailable",
            "Direct image streaming is only available with local file storage. Use resolve-read-urls for Azure Blob.");
    }

    private async Task<bool> SellerOwnsImageKeyAsync(
        Guid sellerId,
        string imageKey,
        CancellationToken cancellationToken)
    {
        var products = await _unitOfWork.Products
            .FindBySellerAsync(sellerId, cancellationToken)
            .ConfigureAwait(false);

        return products.Any(product =>
            product.ImageKeys.Any(key => string.Equals(key, imageKey, StringComparison.OrdinalIgnoreCase)));
    }
}
