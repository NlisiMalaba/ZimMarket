using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZimMarket.API.Http;
using ZimMarket.Application.Admin;
using ZimMarket.Application.Auth;
using ZimMarket.Application.Orders;
using ZimMarket.Domain.Enums;

namespace ZimMarket.API.Controllers.V1;

[ApiController]
[Route("api/v1/admin")]
public sealed class AdminController : ControllerBase
{
    private readonly ISender _sender;

    public AdminController(ISender sender)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
    }

    /// <summary>Lists users with KYC pending review for the given role (seller or driver), including short-lived read SAS URLs for documents.</summary>
    [HttpGet("kyc/pending")]
    [Authorize(Policy = AuthorizationPolicies.Admin)]
    public async Task<IActionResult> GetPendingKyc(
        [FromQuery] UserRole role,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new GetPendingKycQuery(role, page, pageSize);
        return (await _sender.Send(query, cancellationToken).ConfigureAwait(false))
            .ToOkActionResult(HttpContext);
    }

    /// <summary>Approves KYC for a seller or driver in pending review; persists changes and raises approval notifications.</summary>
    [HttpPost("kyc/approve")]
    [Authorize(Policy = AuthorizationPolicies.Admin)]
    public async Task<IActionResult> ApproveKyc(
        [FromBody] ApproveKycRequest request,
        CancellationToken cancellationToken)
    {
        var command = new ApproveKycCommand(request.UserId, request.Role);
        return (await _sender.Send(command, cancellationToken).ConfigureAwait(false))
            .ToOkActionResult(HttpContext);
    }

    public sealed record ApproveKycRequest(Guid UserId, UserRole Role);

    /// <summary>Rejects KYC for a seller or driver in pending review; persists changes and raises rejection notifications.</summary>
    [HttpPost("kyc/reject")]
    [Authorize(Policy = AuthorizationPolicies.Admin)]
    public async Task<IActionResult> RejectKyc(
        [FromBody] RejectKycRequest request,
        CancellationToken cancellationToken)
    {
        var command = new RejectKycCommand(request.UserId, request.Role, request.Reason);
        return (await _sender.Send(command, cancellationToken).ConfigureAwait(false))
            .ToOkActionResult(HttpContext);
    }

    public sealed record RejectKycRequest(Guid UserId, UserRole Role, string Reason);

    /// <summary>Suspends a product listing (policy violation); removes it from the public marketplace.</summary>
    [HttpPost("products/suspend")]
    [Authorize(Policy = AuthorizationPolicies.Admin)]
    public async Task<IActionResult> SuspendProduct(
        [FromBody] SuspendProductRequest request,
        CancellationToken cancellationToken)
    {
        var command = new SuspendProductCommand(request.ProductId, request.Reason);
        return (await _sender.Send(command, cancellationToken).ConfigureAwait(false))
            .ToOkActionResult(HttpContext);
    }

    public sealed record SuspendProductRequest(Guid ProductId, string Reason);

    /// <summary>Paginated orders across all customers for operations / support (optional status and created-at range).</summary>
    [HttpGet("orders")]
    [Authorize(Policy = AuthorizationPolicies.Admin)]
    public async Task<IActionResult> GetAllOrders(
        [FromQuery] OrderStatus? status = null,
        [FromQuery] DateTimeOffset? dateFrom = null,
        [FromQuery] DateTimeOffset? dateTo = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new GetAllOrdersQuery(status, dateFrom, dateTo, page, pageSize);
        return (await _sender.Send(query, cancellationToken).ConfigureAwait(false))
            .ToOkActionResult(HttpContext);
    }
}
