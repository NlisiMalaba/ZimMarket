using ZimMarket.Application.Common.Abstractions;

namespace ZimMarket.Application.Orders;

public sealed record CancelOrderCommand(Guid OrderId, string Reason) : ICommand;
