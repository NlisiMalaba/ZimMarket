using System.ComponentModel.DataAnnotations;

namespace ZimMarket.Infrastructure.Configuration;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "ZimMarket";

    public string Audience { get; set; } = "ZimMarket";

    /// <summary>RSA private key PEM (PKCS#1 or PKCS#8) for signing access tokens.</summary>
    public string PrivateKeyPem { get; set; } = string.Empty;

    /// <summary>RSA public key PEM for validating access tokens.</summary>
    public string PublicKeyPem { get; set; } = string.Empty;

    [Range(1, 120)]
    public int AccessTokenLifetimeMinutes { get; set; } = 15;

    [Range(1, 365)]
    public int RefreshTokenLifetimeDays { get; set; } = 30;

    [Range(10_000, 500_000)]
    public int RefreshTokenPbkdf2Iterations { get; set; } = 100_000;
}
