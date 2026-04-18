using System.Security.Claims;
using ZimMarket.Domain.Enums;

namespace ZimMarket.Application.Common.Interfaces;

/// <summary>
/// RS256 access tokens and opaque refresh tokens (refresh material is never stored in plaintext).
/// </summary>
public interface IJwtService
{
    /// <summary>Creates a signed JWT (RS256) with 15-minute lifetime (from configuration unless overridden).</summary>
    string GenerateAccessToken(Guid userId, string email, UserRole role, KycStatus kycStatus);

    /// <summary>64 random bytes, base64-encoded (used as the refresh token sent to the client).</summary>
    string GenerateRefreshToken();

    /// <summary>UTC expiry instant for a newly issued refresh token, derived from configured lifetime.</summary>
    DateTimeOffset GetRefreshTokenExpiresAtUtc();

    /// <summary>PBKDF2-SHA256 hash suitable for persisting on <see cref="ZimMarket.Domain.Entities.Users.User.RefreshTokenHash"/>.</summary>
    string HashRefreshTokenForStorage(string refreshToken);

    /// <summary>Verifies a refresh token against a stored PBKDF2 hash produced by <see cref="HashRefreshTokenForStorage"/>.</summary>
    bool VerifyRefreshToken(string refreshToken, string storedHash);

    /// <summary>Validates signature, issuer, audience, lifetime, and algorithm (RS256). Returns null if invalid.</summary>
    ClaimsPrincipal? ValidateAccessToken(string accessToken);
}
