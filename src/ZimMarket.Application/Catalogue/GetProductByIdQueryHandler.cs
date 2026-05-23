using MediatR;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Application.Common.Models;
using ZimMarket.Domain.Entities.Catalogue;
using ZimMarket.Domain.Entities.Users;
using ZimMarket.Domain.Enums;
using ZimMarket.Domain.Interfaces;

namespace ZimMarket.Application.Catalogue;

public sealed class GetProductByIdQueryHandler : IRequestHandler<GetProductByIdQuery, Result<ProductDetailDto>>
{
    private static readonly TimeSpan ImageUrlTtl = TimeSpan.FromHours(24);

    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileStorage _fileStorage;

    public GetProductByIdQueryHandler(
        IUnitOfWork unitOfWork,
        IFileStorage fileStorage)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _fileStorage = fileStorage ?? throw new ArgumentNullException(nameof(fileStorage));
    }

    public async Task<Result<ProductDetailDto>> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        Product? product = await _unitOfWork.Products.GetByIdAsync(request.ProductId, cancellationToken).ConfigureAwait(false);
        if (product is null)
            return Result<ProductDetailDto>.Failure("Products.NotFound", "Product was not found.");

        if (product.Status != ProductStatus.Active)
            return Result<ProductDetailDto>.Failure("Products.NotFound", "Product was not found.");

        Seller? seller = await _unitOfWork.Sellers.GetByIdAsync(product.SellerId, cancellationToken).ConfigureAwait(false);
        if (seller is null)
            return Result<ProductDetailDto>.Failure("Products.SellerNotFound", "Seller profile was not found.");

        Category? category = await _unitOfWork.Categories.GetByIdAsync(product.CategoryId, cancellationToken).ConfigureAwait(false);
        if (category is null)
            return Result<ProductDetailDto>.Failure("Products.CategoryNotFound", "Category was not found.");

        var imageUrls = new List<string>(product.ImageKeys.Count);
        DateTimeOffset expiresAt = DateTimeOffset.UtcNow.Add(ImageUrlTtl);

        foreach (string imageKey in product.ImageKeys)
        {
            string imageUrl = await _fileStorage.GenerateSasUrlAsync(imageKey, expiresAt, cancellationToken).ConfigureAwait(false);
            imageUrls.Add(imageUrl);
        }

        return Result<ProductDetailDto>.Success(new ProductDetailDto
        {
            ProductId = product.Id,
            Title = product.Title,
            Description = product.Description,
            PriceAmount = product.Price.Amount,
            PriceCurrency = product.Price.Currency.ToString(),
            StockQuantity = product.StockQuantity,
            SellerName = seller.BusinessName,
            CategoryId = category.Id,
            CategoryName = category.Name,
            PickupStreet = product.PickupAddress.Street,
            PickupSuburb = product.PickupAddress.Suburb,
            PickupCity = product.PickupAddress.City,
            PickupCountry = product.PickupAddress.Country,
            ImageUrls = imageUrls
        });
    }
}
