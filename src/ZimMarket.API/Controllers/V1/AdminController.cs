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
[Authorize(Policy = AuthorizationPolicies.AdminOrAbove)]
public sealed class AdminController : ControllerBase
{
    private readonly ISender _sender;

    public AdminController(ISender sender)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
    }

    /// <summary>Lists users with KYC pending review for the given role (seller or driver), including short-lived read SAS URLs for documents.</summary>
    [HttpGet("kyc")]
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
    [HttpPost("kyc/{userId:guid}/approve")]
    public async Task<IActionResult> ApproveKyc(
        Guid userId,
        [FromBody] ApproveKycRequest request,
        CancellationToken cancellationToken)
    {
        var command = new ApproveKycCommand(userId, request.Role);
        return (await _sender.Send(command, cancellationToken).ConfigureAwait(false))
            .ToOkActionResult(HttpContext);
    }

    public sealed record ApproveKycRequest(UserRole Role);

    /// <summary>Rejects KYC for a seller or driver in pending review; persists changes and raises rejection notifications.</summary>
    [HttpPost("kyc/{userId:guid}/reject")]
    public async Task<IActionResult> RejectKyc(
        Guid userId,
        [FromBody] RejectKycRequest request,
        CancellationToken cancellationToken)
    {
        var command = new RejectKycCommand(userId, request.Role, request.Reason);
        return (await _sender.Send(command, cancellationToken).ConfigureAwait(false))
            .ToOkActionResult(HttpContext);
    }

    public sealed record RejectKycRequest(UserRole Role, string Reason);

    /// <summary>Suspends a product listing (policy violation); removes it from the public marketplace.</summary>
    [HttpPatch("products/{productId:guid}/suspend")]
    public async Task<IActionResult> SuspendProduct(
        Guid productId,
        [FromBody] SuspendProductRequest request,
        CancellationToken cancellationToken)
    {
        var command = new SuspendProductCommand(productId, request.Reason);
        return (await _sender.Send(command, cancellationToken).ConfigureAwait(false))
            .ToOkActionResult(HttpContext);
    }

    public sealed record SuspendProductRequest(string Reason);

    /// <summary>Paginated orders across all customers for operations / support (optional status and created-at range).</summary>
    [HttpGet("orders")]
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

    /// <summary>Forces an order to a new status outside the normal lifecycle (manual intervention).</summary>
    [HttpPatch("orders/{orderId:guid}/status")]
    public async Task<IActionResult> OverrideOrderStatus(
        Guid orderId,
        [FromBody] OverrideOrderStatusRequest request,
        CancellationToken cancellationToken)
    {
        var command = new OverrideOrderStatusCommand(orderId, request.NewStatus, request.Reason);
        return (await _sender.Send(command, cancellationToken).ConfigureAwait(false))
            .ToOkActionResult(HttpContext);
    }

    public sealed record OverrideOrderStatusRequest(OrderStatus NewStatus, string Reason);

    /// <summary>Aggregate operational metrics for the current UTC day (orders placed, paid revenue in USD, drivers on duty, KYC backlog, low stock SKUs).</summary>
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboardStats(CancellationToken cancellationToken = default)
    {
        return (await _sender.Send(new GetDashboardStatsQuery(), cancellationToken).ConfigureAwait(false))
            .ToOkActionResult(HttpContext);
    }

    /// <summary>Creates a new platform administrator and emails them their sign-in credentials.</summary>
    [HttpPost("admins")]
    [Authorize(Policy = AuthorizationPolicies.SuperAdmin)]
    public async Task<IActionResult> CreateAdmin(
        [FromBody] CreateAdminRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateAdminCommand(request.Email, request.Password, request.FullName);
        return (await _sender.Send(command, cancellationToken).ConfigureAwait(false))
            .ToCreatedActionResult(HttpContext);
    }

    public sealed record CreateAdminRequest(string Email, string Password, string FullName);

    /// <summary>Deactivates a user account, revokes refresh tokens, and blocks sign-in until reactivated.</summary>
    [HttpPost("users/{userId:guid}/deactivate")]
    public async Task<IActionResult> DeactivateUser(Guid userId, CancellationToken cancellationToken)
    {
        return (await _sender.Send(new DeactivateUserCommand(userId), cancellationToken).ConfigureAwait(false))
            .ToOkActionResult(HttpContext);
    }

    /// <summary>Reactivates a previously deactivated user account (refresh tokens remain cleared until next login).</summary>
    [HttpPost("users/{userId:guid}/activate")]
    public async Task<IActionResult> ActivateUser(Guid userId, CancellationToken cancellationToken)
    {
        return (await _sender.Send(new ActivateUserCommand(userId), cancellationToken).ConfigureAwait(false))
            .ToOkActionResult(HttpContext);
    }
}
