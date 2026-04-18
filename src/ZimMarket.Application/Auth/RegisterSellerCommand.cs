using ZimMarket.Application.Common.Abstractions;

namespace ZimMarket.Application.Auth;

public sealed record RegisterSellerCommand(
    string Email,
    string Phone,
    string Password,
    string FullName,
    string BusinessName) : ICommand<AuthTokensDto>;
