using ZimMarket.Application.Common.Abstractions;

namespace ZimMarket.Application.Admin;

public sealed record ActivateUserCommand(Guid UserId) : ICommand;
