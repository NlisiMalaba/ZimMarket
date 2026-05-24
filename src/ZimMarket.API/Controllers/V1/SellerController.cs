using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZimMarket.API.Http;
using ZimMarket.Application.Auth;
using ZimMarket.Application.Catalogue;
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

    /// <summary>Account and business profile for the signed-in seller.</summary>
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile(CancellationToken cancellationToken)
    {
        return (await _sender.Send(new GetSellerProfileQuery(), cancellationToken).ConfigureAwait(false))
            .ToOkActionResult(HttpContext);
    }

    /// <summary>Updates business contact details, profile photo, and default pickup address.</summary>
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile(
        [FromBody] UpdateSellerProfileRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateSellerProfileCommand(
            request.FullName,
            request.Email,
            request.Phone,
            request.BusinessName,
            request.ProfilePhotoKey,
            request.DefaultPickupAddress,
            request.ClearDefaultPickupAddress);

        return (await _sender.Send(command, cancellationToken).ConfigureAwait(false))
            .ToOkActionResult(HttpContext);
    }

    /// <summary>Changes the signed-in seller password (invalidates refresh tokens).</summary>
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangeSellerPasswordRequest request,
        CancellationToken cancellationToken)
    {
        return (await _sender.Send(
                new ChangeSellerPasswordCommand(request.CurrentPassword, request.NewPassword),
                cancellationToken)
            .ConfigureAwait(false))
            .ToOkActionResult(HttpContext);
    }

    public sealed record UpdateSellerProfileRequest(
        string FullName,
        string Email,
        string Phone,
        string BusinessName,
        string? ProfilePhotoKey,
        PickupAddressDto? DefaultPickupAddress,
        bool ClearDefaultPickupAddress);

    public sealed record ChangeSellerPasswordRequest(string CurrentPassword, string NewPassword);
}
