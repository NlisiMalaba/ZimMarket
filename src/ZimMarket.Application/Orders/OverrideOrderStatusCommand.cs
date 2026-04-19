using ZimMarket.Application.Common.Abstractions;
using ZimMarket.Domain.Enums;

namespace ZimMarket.Application.Orders;

public sealed record OverrideOrderStatusCommand(Guid OrderId, OrderStatus NewStatus, string Reason) : ICommand;
