using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZimMarket.API.Http;
using ZimMarket.Application.Auth;
using ZimMarket.Application.Catalogue;

namespace ZimMarket.API.Controllers.V1;

[ApiController]
[Route("api/v1/products")]
public sealed class ProductsController : ControllerBase
{
    private readonly ISender _sender;

    public ProductsController(ISender sender)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
    }

    [HttpGet]
    public async Task<IActionResult> SearchProducts(
        [FromQuery] string? searchTerm,
        [FromQuery] Guid? categoryId,
        [FromQuery] decimal? minPriceUsd,
        [FromQuery] decimal? maxPriceUsd,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new SearchProductsQuery(searchTerm, categoryId, minPriceUsd, maxPriceUsd, page, pageSize);
        return (await _sender.Send(query, cancellationToken).ConfigureAwait(false))
            .ToOkActionResult(HttpContext);
    }

    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories(CancellationToken cancellationToken)
    {
        return (await _sender.Send(new GetCategoriesQuery(), cancellationToken).ConfigureAwait(false))
            .ToOkActionResult(HttpContext);
    }

    [HttpGet("my")]
    [Authorize(Policy = AuthorizationPolicies.Seller)]
    public async Task<IActionResult> GetMyProducts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new GetSellerProductsQuery(page, pageSize);
        return (await _sender.Send(query, cancellationToken).ConfigureAwait(false))
            .ToOkActionResult(HttpContext);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        return (await _sender.Send(new GetProductByIdQuery(id), cancellationToken).ConfigureAwait(false))
            .ToOkActionResult(HttpContext);
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.SellerKycApproved)]
    public async Task<IActionResult> CreateProduct(
        [FromBody] CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateProductCommand(
            request.Title,
            request.Description,
            request.PriceUsd,
            request.CategoryId,
            request.StockQuantity,
            request.ImageKeys,
            new PickupAddressDto(
                request.PickupAddress.Street,
                request.PickupAddress.Suburb,
                request.PickupAddress.City,
                request.PickupAddress.Country));

        return (await _sender.Send(command, cancellationToken).ConfigureAwait(false))
            .ToCreatedActionResult(HttpContext);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.SellerKycApproved)]
    public async Task<IActionResult> UpdateProduct(
        Guid id,
        [FromBody] UpdateProductRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateProductCommand(
            id,
            request.Title,
            request.Description,
            request.PriceUsd,
            request.CategoryId,
            request.ImageKeys,
            new PickupAddressDto(
                request.PickupAddress.Street,
                request.PickupAddress.Suburb,
                request.PickupAddress.City,
                request.PickupAddress.Country));

        return (await _sender.Send(command, cancellationToken).ConfigureAwait(false))
            .ToOkActionResult(HttpContext);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.SellerKycApproved)]
    public async Task<IActionResult> DeleteProduct(Guid id, CancellationToken cancellationToken)
    {
        return (await _sender.Send(new DeleteProductCommand(id), cancellationToken).ConfigureAwait(false))
            .ToOkActionResult(HttpContext);
    }

    [HttpPatch("{id:guid}/stock")]
    [Authorize(Policy = AuthorizationPolicies.SellerKycApproved)]
    public async Task<IActionResult> UpdateStock(
        Guid id,
        [FromBody] UpdateStockRequest request,
        CancellationToken cancellationToken)
    {
        return (await _sender.Send(new UpdateStockCommand(id, request.Delta), cancellationToken).ConfigureAwait(false))
            .ToOkActionResult(HttpContext);
    }

    public sealed record CreateProductRequest(
        string Title,
        string Description,
        decimal PriceUsd,
        Guid CategoryId,
        int StockQuantity,
        IReadOnlyList<string> ImageKeys,
        PickupAddressRequest PickupAddress);

    public sealed record UpdateProductRequest(
        string Title,
        string Description,
        decimal PriceUsd,
        Guid CategoryId,
        IReadOnlyList<string> ImageKeys,
        PickupAddressRequest PickupAddress);

    public sealed record UpdateStockRequest(int Delta);

    public sealed record PickupAddressRequest(
        string Street,
        string Suburb,
        string City,
        string Country);
}
