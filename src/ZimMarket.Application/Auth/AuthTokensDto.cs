namespace ZimMarket.Application.Auth;

public sealed class AuthTokensDto
{
    public required string AccessToken { get; init; }

    public required string RefreshToken { get; init; }
}
