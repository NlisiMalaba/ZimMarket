using MediatR;
using Microsoft.Extensions.Logging;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Application.Common.Models;
using ZimMarket.Domain.Common;
using ZimMarket.Domain.Entities.Catalogue;
using ZimMarket.Domain.Entities.Users;
using ZimMarket.Domain.Enums;
using ZimMarket.Domain.Interfaces;

namespace ZimMarket.Application.Catalogue;

public sealed class SearchProductsQueryHandler : IRequestHandler<SearchProductsQuery, Result<ZimMarket.Shared.PagedList<ProductSummaryDto>>>
{
    private static readonly TimeSpan ImageUrlTtl = TimeSpan.FromHours(24);

    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileStorage _fileStorage;
    private readonly ILogger<SearchProductsQueryHandler> _logger;

    public SearchProductsQueryHandler(
        IUnitOfWork unitOfWork,
        IFileStorage fileStorage,
        ILogger<SearchProductsQueryHandler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _fileStorage = fileStorage ?? throw new ArgumentNullException(nameof(fileStorage));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<ZimMarket.Shared.PagedList<ProductSummaryDto>>> Handle(SearchProductsQuery request, CancellationToken cancellationToken)
    {
        var filter = new ProductFilter(
            request.SearchTerm,
            request.CategoryId,
            request.MinPriceUsd,
            request.MaxPriceUsd,
            SellerId: null);

        var pagination = new ZimMarket.Shared.PaginationParams
        {
            Page = request.Page,
            PageSize = request.PageSize
        };

        ZimMarket.Shared.PagedList<Product> products = await _unitOfWork.Products
            .GetPagedAsync(filter, pagination, cancellationToken)
            .ConfigureAwait(false);

        var sellerNames = new Dictionary<Guid, string>();
        var categoryNames = new Dictionary<Guid, string>();
        var summaries = new List<ProductSummaryDto>(products.Items.Count);
        DateTimeOffset imageExpiry = DateTimeOffset.UtcNow.Add(ImageUrlTtl);

        foreach (Product product in products.Items)
        {
            string sellerName = await ResolveSellerNameAsync(product.SellerId, sellerNames, cancellationToken).ConfigureAwait(false);
            string categoryName = await ResolveCategoryNameAsync(product.CategoryId, categoryNames, cancellationToken).ConfigureAwait(false);
            string? primaryImageUrl = await ResolvePrimaryImageUrlAsync(product.ImageKeys, imageExpiry, cancellationToken).ConfigureAwait(false);

            summaries.Add(new ProductSummaryDto
            {
                ProductId = product.Id,
                Status = ProductStatus.Active,
                Title = product.Title,
                Description = ProductDescriptionFormatter.Truncate(product.Description),
                PriceAmount = product.Price.Amount,
                PriceCurrency = product.Price.Currency.ToString(),
                StockQuantity = product.StockQuantity,
                SellerId = product.SellerId,
                SellerName = sellerName,
                CategoryId = product.CategoryId,
                CategoryName = categoryName,
                PrimaryImageUrl = primaryImageUrl,
                UpdatedAt = product.UpdatedAt,
                CreatedAt = product.CreatedAt,
            });
        }

        return Result<ZimMarket.Shared.PagedList<ProductSummaryDto>>.Success(
            new ZimMarket.Shared.PagedList<ProductSummaryDto>(summaries, products.Page, products.PageSize, products.TotalCount));
    }

    private async Task<string> ResolveSellerNameAsync(
        Guid sellerId,
        Dictionary<Guid, string> sellerNames,
        CancellationToken cancellationToken)
    {
        if (sellerNames.TryGetValue(sellerId, out string? cached))
            return cached;

        Seller? seller = await _unitOfWork.Sellers.GetByIdAsync(sellerId, cancellationToken).ConfigureAwait(false);
        string sellerName = seller?.BusinessName ?? "Unknown seller";
        sellerNames[sellerId] = sellerName;
        return sellerName;
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

        try
        {
            return await _fileStorage.GenerateSasUrlAsync(imageKeys[0], expiry, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException or FormatException)
        {
            _logger.LogWarning(ex, "Product primary image URL generation failed for blob key {BlobKey}.", imageKeys[0]);
            return null;
        }
    }
}
