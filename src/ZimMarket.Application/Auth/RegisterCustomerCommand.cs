using ZimMarket.Application.Common.Abstractions;

namespace ZimMarket.Application.Auth;

public sealed record RegisterCustomerCommand(
    string Email,
    string Phone,
    string Password,
    string FullName,
    string? PushToken) : ICommand<AuthTokensDto>;
