using ZimMarket.Application.Common.Abstractions;

namespace ZimMarket.Application.Auth;

public sealed record LogoutCommand(string RefreshToken) : ICommand;
