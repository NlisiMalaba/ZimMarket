using MediatR;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Application.Common.Models;
using ZimMarket.Domain.Entities.Catalogue;
using ZimMarket.Domain.Enums;
using ZimMarket.Domain.Interfaces;

namespace ZimMarket.Application.Catalogue;

public sealed class GetSellerProductDetailQueryHandler
    : IRequestHandler<GetSellerProductDetailQuery, Result<SellerProductDetailDto>>
{
    private static readonly TimeSpan ImageUrlTtl = TimeSpan.FromHours(1);

    private readonly ICurrentUser _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileStorage _fileStorage;

    public GetSellerProductDetailQueryHandler(
        ICurrentUser currentUser,
        IUnitOfWork unitOfWork,
        IFileStorage fileStorage)
    {
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _fileStorage = fileStorage ?? throw new ArgumentNullException(nameof(fileStorage));
    }

    public async Task<Result<SellerProductDetailDto>> Handle(
        GetSellerProductDetailQuery request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.Role != UserRole.Seller || _currentUser.UserId == Guid.Empty)
        {
            return Result<SellerProductDetailDto>.Failure(
                "Products.Forbidden",
                "Only authenticated sellers can view seller products.");
        }

        Product? product = await _unitOfWork.Products
            .GetByIdIncludingDeletedAsync(request.ProductId, cancellationToken)
            .ConfigureAwait(false);

        if (product is null || product.SellerId != _currentUser.UserId)
            return Result<SellerProductDetailDto>.Failure("Products.NotFound", "Product was not found.");

        Category? category = await _unitOfWork.Categories
            .GetByIdAsync(product.CategoryId, cancellationToken)
            .ConfigureAwait(false);

        var imageUrls = new List<string>(product.ImageKeys.Count);
        DateTimeOffset imageExpiry = DateTimeOffset.UtcNow.Add(ImageUrlTtl);

        foreach (string imageKey in product.ImageKeys)
        {
            string imageUrl = await _fileStorage
                .GenerateSasUrlAsync(imageKey, imageExpiry, cancellationToken)
                .ConfigureAwait(false);
            imageUrls.Add(imageUrl);
        }

        return Result<SellerProductDetailDto>.Success(new SellerProductDetailDto
        {
            ProductId = product.Id,
            Status = product.Status,
            Title = product.Title,
            Description = product.Description,
            PriceAmount = product.Price.Amount,
            PriceCurrency = product.Price.Currency.ToString(),
            StockQuantity = product.StockQuantity,
            CategoryId = product.CategoryId,
            CategoryName = category?.Name ?? "Unknown category",
            PickupStreet = product.PickupAddress.Street,
            PickupSuburb = product.PickupAddress.Suburb,
            PickupCity = product.PickupAddress.City,
            PickupCountry = product.PickupAddress.Country,
            ImageKeys = product.ImageKeys.ToList(),
            ImageUrls = imageUrls,
            UpdatedAt = product.UpdatedAt,
        });
    }
}
