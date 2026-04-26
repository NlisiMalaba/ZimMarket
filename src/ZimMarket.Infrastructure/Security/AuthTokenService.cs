using System.Security.Cryptography;
using System.Text;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Domain.Enums;

namespace ZimMarket.Infrastructure.Security;

public sealed class AuthTokenService : IAuthTokenService
{
    private static readonly TimeSpan EmailVerificationLifetime = TimeSpan.FromHours(24);
    private static readonly TimeSpan PasswordResetLifetime = TimeSpan.FromMinutes(30);

    public string GenerateRawToken()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public string HashToken(string rawToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rawToken);

        byte[] bytes = Encoding.UTF8.GetBytes(rawToken.Trim());
        byte[] hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public DateTimeOffset GetExpiry(AuthTokenPurpose purpose, DateTimeOffset fromUtc)
    {
        return purpose switch
        {
            AuthTokenPurpose.AdminEmailVerification => fromUtc.Add(EmailVerificationLifetime),
            AuthTokenPurpose.PasswordReset => fromUtc.Add(PasswordResetLifetime),
            _ => fromUtc.AddMinutes(30)
        };
    }
}
