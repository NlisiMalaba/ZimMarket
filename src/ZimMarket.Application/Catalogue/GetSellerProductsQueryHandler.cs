using MediatR;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Application.Common.Models;
using ZimMarket.Domain.Common;
using ZimMarket.Domain.Entities.Catalogue;
using ZimMarket.Domain.Interfaces;
using ZimMarket.Domain.Enums;

namespace ZimMarket.Application.Catalogue;

public sealed class GetSellerProductsQueryHandler : IRequestHandler<GetSellerProductsQuery, Result<ZimMarket.Shared.PagedList<ProductSummaryDto>>>
{
    private static readonly TimeSpan ImageUrlTtl = TimeSpan.FromHours(1);

    private readonly ICurrentUser _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileStorage _fileStorage;

    public GetSellerProductsQueryHandler(
        ICurrentUser currentUser,
        IUnitOfWork unitOfWork,
        IFileStorage fileStorage)
    {
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _fileStorage = fileStorage ?? throw new ArgumentNullException(nameof(fileStorage));
    }

    public async Task<Result<ZimMarket.Shared.PagedList<ProductSummaryDto>>> Handle(GetSellerProductsQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.Role != UserRole.Seller || _currentUser.UserId == Guid.Empty)
            return Result<ZimMarket.Shared.PagedList<ProductSummaryDto>>.Failure("Products.Forbidden", "Only authenticated sellers can view seller products.");

        var filter = new ProductFilter(
            SearchTerm: null,
            CategoryId: null,
            MinPriceUsd: null,
            MaxPriceUsd: null,
            SellerId: _currentUser.UserId);

        var pagination = new ZimMarket.Shared.PaginationParams
        {
            Page = request.Page,
            PageSize = request.PageSize
        };

        ZimMarket.Shared.PagedList<Product> products = await _unitOfWork.Products
            .GetPagedAsync(filter, pagination, cancellationToken)
            .ConfigureAwait(false);

        var categoryNames = new Dictionary<Guid, string>();
        var summaries = new List<ProductSummaryDto>(products.Items.Count);
        DateTimeOffset imageExpiry = DateTimeOffset.UtcNow.Add(ImageUrlTtl);

        foreach (Product product in products.Items)
        {
            string categoryName = await ResolveCategoryNameAsync(product.CategoryId, categoryNames, cancellationToken).ConfigureAwait(false);
            string? primaryImageUrl = await ResolvePrimaryImageUrlAsync(product.ImageKeys, imageExpiry, cancellationToken).ConfigureAwait(false);

            summaries.Add(new ProductSummaryDto
            {
                ProductId = product.Id,
                Title = product.Title,
                PriceAmount = product.Price.Amount,
                PriceCurrency = product.Price.Currency.ToString(),
                StockQuantity = product.StockQuantity,
                SellerId = product.SellerId,
                SellerName = "You",
                CategoryId = product.CategoryId,
                CategoryName = categoryName,
                PrimaryImageUrl = primaryImageUrl
            });
        }

        return Result<ZimMarket.Shared.PagedList<ProductSummaryDto>>.Success(
            new ZimMarket.Shared.PagedList<ProductSummaryDto>(summaries, products.Page, products.PageSize, products.TotalCount));
    }

    private async Task<string> ResolveCategoryNameAsync(
        Guid categoryId,
        Dictionary<Guid, string> categoryNames,
        CancellationToken cancellationToken)
    {
        if (categoryNames.TryGetValue(categoryId, out string? cached))
            return cached;

        Category? category = await _unitOfWork.Categories.GetByIdAsync(categoryId, cancellationToken).ConfigureAwait(false);
        string categoryName = category?.Name ?? "Unknown category";
        categoryNames[categoryId] = categoryName;
        return categoryName;
    }

    private async Task<string?> ResolvePrimaryImageUrlAsync(
        IReadOnlyList<string> imageKeys,
        DateTimeOffset expiry,
        CancellationToken cancellationToken)
    {
        if (imageKeys.Count == 0)
            return null;

        return await _fileStorage.GenerateSasUrlAsync(imageKeys[0], expiry, cancellationToken).ConfigureAwait(false);
    }
}
