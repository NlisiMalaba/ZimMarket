using ZimMarket.Domain.Enums;

namespace ZimMarket.Application.Common.Interfaces;

public interface IAuthTokenService
{
    string GenerateRawToken();

    string HashToken(string rawToken);

    DateTimeOffset GetExpiry(AuthTokenPurpose purpose, DateTimeOffset fromUtc);
}
