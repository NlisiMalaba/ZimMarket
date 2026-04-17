using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
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
}
