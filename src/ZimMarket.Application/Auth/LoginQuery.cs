using ZimMarket.Application.Common.Abstractions;

namespace ZimMarket.Application.Auth;

public sealed record LoginQuery(string Email, string Password, string? DeviceInfo) : IQuery<AuthTokensDto>;
