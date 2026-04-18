using ZimMarket.Application.Common.Abstractions;

namespace ZimMarket.Application.Auth;

public sealed record RegisterDriverCommand(
    string Email,
    string Phone,
    string Password,
    string FullName) : ICommand<AuthTokensDto>;
