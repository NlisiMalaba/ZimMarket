using ZimMarket.Application.Common.Abstractions;

namespace ZimMarket.Application.Auth;

public sealed record RefreshTokenCommand(string AccessToken, string RefreshToken) : ICommand<AuthTokensDto>;
