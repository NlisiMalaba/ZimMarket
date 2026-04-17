using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Domain.Enums;
using ZimMarket.Infrastructure.Configuration;

namespace ZimMarket.Infrastructure.Security;

public sealed class JwtService : IJwtService
{
    private const string RefreshTokenHashPrefix = "v1.";
    private const int RefreshTokenRandomByteCount = 64;

    private readonly JwtOptions _options;
    private readonly ILogger<JwtService> _logger;
    private readonly JwtSecurityTokenHandler _tokenHandler = new() { MapInboundClaims = false };
    private readonly Lazy<RsaSecurityKey> _signingKey;
    private readonly Lazy<RsaSecurityKey> _validationKey;
    private readonly Lazy<TokenValidationParameters> _validationParameters;

    public JwtService(IOptions<JwtOptions> options, ILogger<JwtService> logger)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _signingKey = new Lazy<RsaSecurityKey>(() => CreateRsaSecurityKeyFromPrivatePem(_options.PrivateKeyPem));
        _validationKey = new Lazy<RsaSecurityKey>(() => CreateRsaSecurityKeyFromPublicPem(_options.PublicKeyPem));
        _validationParameters = new Lazy<TokenValidationParameters>(() => new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = _validationKey.Value,
            ValidateIssuer = true,
            ValidIssuer = _options.Issuer,
            ValidateAudience = true,
            ValidAudience = _options.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            ValidAlgorithms = [SecurityAlgorithms.RsaSha256]
        });
    }

    public string GenerateAccessToken(Guid userId, string email, UserRole role, KycStatus kycStatus)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        var jti = Guid.NewGuid().ToString("N");
        var now = DateTimeOffset.UtcNow;
        var expires = now.AddMinutes(_options.AccessTokenLifetimeMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString("D")),
            new(JwtRegisteredClaimNames.Email, email),
            new("role", role.ToString()),
            new("kycStatus", kycStatus.ToString()),
            new(JwtRegisteredClaimNames.Jti, jti),
            new(JwtRegisteredClaimNames.Iat, now.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture), ClaimValueTypes.Integer64)
        };

        var credentials = new SigningCredentials(_signingKey.Value, SecurityAlgorithms.RsaSha256);
        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expires.UtcDateTime,
            signingCredentials: credentials);

        return _tokenHandler.WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        Span<byte> buffer = stackalloc byte[RefreshTokenRandomByteCount];
        RandomNumberGenerator.Fill(buffer);
        return Convert.ToBase64String(buffer);
    }

    public string HashRefreshTokenForStorage(string refreshToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(refreshToken);

        Span<byte> salt = stackalloc byte[16];
        RandomNumberGenerator.Fill(salt);

        byte[] subkey = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(refreshToken),
            salt,
            _options.RefreshTokenPbkdf2Iterations,
            HashAlgorithmName.SHA256,
            32);

        return string.Concat(
            RefreshTokenHashPrefix,
            _options.RefreshTokenPbkdf2Iterations.ToString(CultureInfo.InvariantCulture),
            ".",
            Convert.ToBase64String(salt),
            ".",
            Convert.ToBase64String(subkey));
    }

    public bool VerifyRefreshToken(string refreshToken, string storedHash)
    {
        if (string.IsNullOrWhiteSpace(refreshToken) || string.IsNullOrWhiteSpace(storedHash))
            return false;

        if (!storedHash.StartsWith(RefreshTokenHashPrefix, StringComparison.Ordinal))
            return false;

        string remainder = storedHash[RefreshTokenHashPrefix.Length..];
        string[] parts = remainder.Split('.', 3);
        if (parts.Length != 3)
            return false;

        if (!int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int iterations)
            || iterations < 10_000)
            return false;

        byte[] salt;
        byte[] expectedSubkey;
        try
        {
            salt = Convert.FromBase64String(parts[1]);
            expectedSubkey = Convert.FromBase64String(parts[2]);
        }
        catch (FormatException)
        {
            return false;
        }

        byte[] actualSubkey = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(refreshToken),
            salt,
            iterations,
            HashAlgorithmName.SHA256,
            expectedSubkey.Length);

        return CryptographicOperations.FixedTimeEquals(actualSubkey, expectedSubkey);
    }

    public ClaimsPrincipal? ValidateAccessToken(string accessToken)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
            return null;

        try
        {
            ClaimsPrincipal principal = _tokenHandler.ValidateToken(
                accessToken,
                _validationParameters.Value,
                out SecurityToken _);

            return principal;
        }
        catch (SecurityTokenException ex)
        {
            _logger.LogDebug(ex, "Access token validation failed.");
            return null;
        }
        catch (ArgumentException ex)
        {
            _logger.LogDebug(ex, "Access token validation failed.");
            return null;
        }
    }

    private static RsaSecurityKey CreateRsaSecurityKeyFromPrivatePem(string pem)
    {
        if (string.IsNullOrWhiteSpace(pem))
        {
            throw new InvalidOperationException(
                "JWT signing is not configured: set Jwt:PrivateKeyPem (RSA private key PEM) in configuration or environment variables.");
        }

        RSA rsa = RSA.Create();
        rsa.ImportFromPem(pem);
        return new RsaSecurityKey(rsa);
    }

    private static RsaSecurityKey CreateRsaSecurityKeyFromPublicPem(string pem)
    {
        if (string.IsNullOrWhiteSpace(pem))
        {
            throw new InvalidOperationException(
                "JWT validation is not configured: set Jwt:PublicKeyPem (RSA public key PEM) in configuration or environment variables.");
        }

        RSA rsa = RSA.Create();
        rsa.ImportFromPem(pem);
        return new RsaSecurityKey(rsa);
    }
}
