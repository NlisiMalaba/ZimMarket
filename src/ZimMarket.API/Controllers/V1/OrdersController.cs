using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZimMarket.API.Http;
using ZimMarket.Application.Auth;
using ZimMarket.Application.Orders;
using ZimMarket.Domain.Enums;

namespace ZimMarket.API.Controllers.V1;

[ApiController]
[Route("api/v1/orders")]
public sealed class OrdersController : ControllerBase
{
    private readonly ISender _sender;

    public OrdersController(ISender sender)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.Customer)]
    public async Task<IActionResult> PlaceOrder(
        [FromBody] PlaceOrderRequest request,
        CancellationToken cancellationToken)
    {
        var command = new PlaceOrderCommand(
            request.Items.Select(x => new PlaceOrderItemDto(x.ProductId, x.Quantity)).ToList(),
            new PlaceOrderDeliveryAddressDto(
                request.DeliveryAddress.Street,
                request.DeliveryAddress.Suburb,
                request.DeliveryAddress.City,
                request.DeliveryAddress.Country),
            request.PaymentMethod);

        return (await _sender.Send(command, cancellationToken).ConfigureAwait(false))
            .ToCreatedActionResult(HttpContext);
    }

    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.Customer)]
    public async Task<IActionResult> GetCustomerOrders(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] OrderStatus? statusFilter = null,
        CancellationToken cancellationToken = default)
    {
        var query = new GetCustomerOrdersQuery(page, pageSize, statusFilter);
        return (await _sender.Send(query, cancellationToken).ConfigureAwait(false))
            .ToOkActionResult(HttpContext);
    }

    [HttpGet("seller")]
    [Authorize(Policy = AuthorizationPolicies.Seller)]
    public async Task<IActionResult> GetSellerOrders(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] OrderStatus? statusFilter = null,
        CancellationToken cancellationToken = default)
    {
        var query = new GetSellerOrdersQuery(page, pageSize, statusFilter);
        return (await _sender.Send(query, cancellationToken).ConfigureAwait(false))
            .ToOkActionResult(HttpContext);
    }

    [HttpGet("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> GetOrderById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var query = new GetOrderByIdQuery(id);
        return (await _sender.Send(query, cancellationToken).ConfigureAwait(false))
            .ToOkActionResult(HttpContext);
    }

    [HttpGet("seller/{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.Seller)]
    public async Task<IActionResult> GetSellerOrderById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var query = new GetSellerOrderDetailQuery(id);
        return (await _sender.Send(query, cancellationToken).ConfigureAwait(false))
            .ToOkActionResult(HttpContext);
    }

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Policy = AuthorizationPolicies.Customer)]
    public async Task<IActionResult> CancelOrder(
        Guid id,
        [FromBody] CancelOrderRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CancelOrderCommand(id, request.Reason);
        return (await _sender.Send(command, cancellationToken).ConfigureAwait(false))
            .ToOkActionResult(HttpContext);
    }

    public sealed record PlaceOrderRequest(
        IReadOnlyList<PlaceOrderItemRequest> Items,
        PlaceOrderAddressRequest DeliveryAddress,
        PaymentMethod PaymentMethod);

    public sealed record PlaceOrderItemRequest(Guid ProductId, int Quantity);

    public sealed record PlaceOrderAddressRequest(
        string Street,
        string Suburb,
        string City,
        string Country);

    public sealed record CancelOrderRequest(string Reason);
}
