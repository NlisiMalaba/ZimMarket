using ZimMarket.Application.Common.Abstractions;

namespace ZimMarket.Application.Admin;

public sealed record DeactivateUserCommand(Guid UserId) : ICommand;
