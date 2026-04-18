using System.Security.Claims;

namespace ZimMarket.Application.Common.Models;

/// <summary>
/// Result of validating an access token for refresh: signature/issuer/audience are valid; carries expiry for "must be expired" checks.
/// </summary>
public sealed record AccessTokenForRefreshPrincipal(
    ClaimsPrincipal Principal,
    DateTimeOffset AccessTokenExpiresAtUtc);
