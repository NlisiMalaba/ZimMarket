using ZimMarket.Application.Common.Abstractions;

namespace ZimMarket.Application.Admin;

public sealed record SuspendProductCommand(Guid ProductId, string Reason) : ICommand;
