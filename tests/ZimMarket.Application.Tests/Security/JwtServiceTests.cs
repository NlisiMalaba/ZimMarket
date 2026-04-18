using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ZimMarket.Application.Common.Models;
using ZimMarket.Domain.Enums;
using ZimMarket.Infrastructure.Configuration;
using ZimMarket.Infrastructure.Security;

namespace ZimMarket.Application.Tests.Security;

public sealed class JwtServiceTests
{
    private static JwtService CreateSut()
    {
        using RSA rsa = RSA.Create(2048);
        string privatePem = rsa.ExportPkcs8PrivateKeyPem();
        string publicPem = rsa.ExportSubjectPublicKeyInfoPem();

        IOptions<JwtOptions> options = Options.Create(new JwtOptions
        {
            Issuer = "test-issuer",
            Audience = "test-audience",
            PrivateKeyPem = privatePem,
            PublicKeyPem = publicPem,
            AccessTokenLifetimeMinutes = 15,
            RefreshTokenPbkdf2Iterations = 50_000
        });

        return new JwtService(options, NullLogger<JwtService>.Instance);
    }

    [Fact]
    public void GenerateAccessToken_Then_ValidateAccessToken_ReturnsPrincipalWithClaims()
    {
        JwtService sut = CreateSut();
        Guid userId = Guid.NewGuid();
        string token = sut.GenerateAccessToken(userId, "user@example.com", UserRole.Customer, KycStatus.Approved);

        var principal = sut.ValidateAccessToken(token);
        Assert.NotNull(principal);
        Assert.Equal(userId.ToString("D"), principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value);
        Assert.Equal("user@example.com", principal.FindFirst(JwtRegisteredClaimNames.Email)?.Value);
        Assert.Equal(UserRole.Customer.ToString(), principal.FindFirst("role")?.Value);
        Assert.Equal(KycStatus.Approved.ToString(), principal.FindFirst("kycStatus")?.Value);
        Assert.False(string.IsNullOrEmpty(principal.FindFirst(JwtRegisteredClaimNames.Jti)?.Value));
    }

    [Fact]
    public void HashRefreshToken_Then_VerifyRefreshToken_Succeeds_AndWrongSecretFails()
    {
        JwtService sut = CreateSut();
        string refresh = sut.GenerateRefreshToken();
        string hash = sut.HashRefreshTokenForStorage(refresh);

        Assert.True(sut.VerifyRefreshToken(refresh, hash));
        Assert.False(sut.VerifyRefreshToken(refresh + "x", hash));
    }

    [Fact]
    public void TryValidateAccessTokenForRefresh_on_valid_token_returns_principal_and_expiry()
    {
        JwtService sut = CreateSut();
        Guid userId = Guid.NewGuid();
        string token = sut.GenerateAccessToken(userId, "user@example.com", UserRole.Customer, KycStatus.Approved);

        AccessTokenForRefreshPrincipal? result = sut.TryValidateAccessTokenForRefresh(token);

        Assert.NotNull(result);
        Assert.Equal(userId.ToString("D"), result.Principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value);
        Assert.True(result.AccessTokenExpiresAtUtc > DateTimeOffset.UtcNow);
    }

    [Fact]
    public void TryValidateAccessTokenForRefresh_on_tampered_token_returns_null()
    {
        JwtService sut = CreateSut();
        string token = sut.GenerateAccessToken(Guid.NewGuid(), "user@example.com", UserRole.Customer, KycStatus.Approved);
        string tampered = token[..^4] + "xxxx";

        Assert.Null(sut.TryValidateAccessTokenForRefresh(tampered));
    }
}
