using ZimMarket.Domain.Enums;

namespace ZimMarket.Application.Auth;

public sealed class AuthTokensDto
{
    public required string AccessToken { get; init; }

    public required string RefreshToken { get; init; }

    public required KycStatus KycStatus { get; init; }
}
