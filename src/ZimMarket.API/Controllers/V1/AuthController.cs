using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZimMarket.API.Http;
using ZimMarket.Application.Auth;

namespace ZimMarket.API.Controllers.V1;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly ISender _sender;

    public AuthController(ISender sender)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
    }

    [HttpPost("register/customer")]
    public async Task<IActionResult> RegisterCustomer(
        [FromBody] RegisterCustomerRequest request,
        CancellationToken cancellationToken)
    {
        var command = new RegisterCustomerCommand(
            request.Email,
            request.Phone,
            request.Password,
            request.FullName,
            request.PushToken);

        return (await _sender.Send(command, cancellationToken).ConfigureAwait(false))
            .ToCreatedActionResult(HttpContext);
    }

    [HttpPost("register/seller")]
    public async Task<IActionResult> RegisterSeller(
        [FromBody] RegisterSellerRequest request,
        CancellationToken cancellationToken)
    {
        var command = new RegisterSellerCommand(
            request.Email,
            request.Phone,
            request.Password,
            request.FullName,
            request.BusinessName);

        return (await _sender.Send(command, cancellationToken).ConfigureAwait(false))
            .ToCreatedActionResult(HttpContext);
    }

    [HttpPost("register/driver")]
    public async Task<IActionResult> RegisterDriver(
        [FromBody] RegisterDriverRequest request,
        CancellationToken cancellationToken)
    {
        var command = new RegisterDriverCommand(
            request.Email,
            request.Phone,
            request.Password,
            request.FullName);

        return (await _sender.Send(command, cancellationToken).ConfigureAwait(false))
            .ToCreatedActionResult(HttpContext);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var query = new LoginQuery(request.Email, request.Password, request.DeviceInfo);

        return (await _sender.Send(query, cancellationToken).ConfigureAwait(false))
            .ToOkActionResult(HttpContext);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request, CancellationToken cancellationToken)
    {
        var command = new RefreshTokenCommand(request.AccessToken, request.RefreshToken);

        return (await _sender.Send(command, cancellationToken).ConfigureAwait(false))
            .ToOkActionResult(HttpContext);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest request, CancellationToken cancellationToken)
    {
        var command = new LogoutCommand(request.RefreshToken);

        return (await _sender.Send(command, cancellationToken).ConfigureAwait(false))
            .ToOkActionResult(HttpContext);
    }

    [HttpPost("kyc/seller")]
    [Authorize(Policy = AuthorizationPolicies.Seller)]
    public async Task<IActionResult> SubmitSellerKyc(
        [FromBody] SubmitSellerKycRequest request,
        CancellationToken cancellationToken)
    {
        var command = new SubmitSellerKycCommand(request.NationalIdKey, request.ProofOfResidenceKey);

        return (await _sender.Send(command, cancellationToken).ConfigureAwait(false))
            .ToOkActionResult(HttpContext);
    }

    [HttpPost("kyc/driver")]
    [Authorize(Policy = AuthorizationPolicies.Driver)]
    public async Task<IActionResult> SubmitDriverKyc(
        [FromBody] SubmitDriverKycRequest request,
        CancellationToken cancellationToken)
    {
        var command = new SubmitDriverKycCommand(
            request.LicenseDocKey,
            request.VehicleDocKey,
            request.LicenseNumber,
            request.VehicleRegistration);

        return (await _sender.Send(command, cancellationToken).ConfigureAwait(false))
            .ToOkActionResult(HttpContext);
    }

    public sealed record RegisterCustomerRequest(
        string Email,
        string Phone,
        string Password,
        string FullName,
        string? PushToken);

    public sealed record RegisterSellerRequest(
        string Email,
        string Phone,
        string Password,
        string FullName,
        string BusinessName);

    public sealed record RegisterDriverRequest(string Email, string Phone, string Password, string FullName);

    public sealed record LoginRequest(string Email, string Password, string? DeviceInfo);

    public sealed record RefreshRequest(string AccessToken, string RefreshToken);

    public sealed record LogoutRequest(string RefreshToken);

    public sealed record SubmitSellerKycRequest(string NationalIdKey, string ProofOfResidenceKey);

    public sealed record SubmitDriverKycRequest(
        string LicenseDocKey,
        string VehicleDocKey,
        string LicenseNumber,
        string VehicleRegistration);
}
