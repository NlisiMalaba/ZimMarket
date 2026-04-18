namespace ZimMarket.Application.Auth;

/// <summary>Claim types issued on access tokens (must match <see cref="ZimMarket.Infrastructure.Security.JwtService"/>).</summary>
public static class AuthClaimTypes
{
    public const string Role = "role";

    public const string KycStatus = "kycStatus";
}
