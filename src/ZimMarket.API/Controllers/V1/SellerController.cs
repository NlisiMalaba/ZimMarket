using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZimMarket.API.Http;
using ZimMarket.Application.Auth;
using ZimMarket.Application.Sellers;

namespace ZimMarket.API.Controllers.V1;

[ApiController]
[Route("api/v1/seller")]
[Authorize(Policy = AuthorizationPolicies.Seller)]
public sealed class SellerController : ControllerBase
{
    private readonly ISender _sender;

    public SellerController(ISender sender)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
    }

    /// <summary>Aggregate seller store metrics (revenue, orders, listings, low stock).</summary>
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboardStats(CancellationToken cancellationToken)
    {
        return (await _sender.Send(new GetSellerDashboardStatsQuery(), cancellationToken).ConfigureAwait(false))
            .ToOkActionResult(HttpContext);
    }

    /// <summary>Current seller KYC status and rejection reason (if any).</summary>
    [HttpGet("verification")]
    public async Task<IActionResult> GetVerification(CancellationToken cancellationToken)
    {
        return (await _sender.Send(new GetSellerVerificationQuery(), cancellationToken).ConfigureAwait(false))
            .ToOkActionResult(HttpContext);
    }
}
