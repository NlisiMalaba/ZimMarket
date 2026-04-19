using ZimMarket.Application.Common.Abstractions;

namespace ZimMarket.Application.Warehouse;

public sealed record RecordItemArrivalCommand(Guid OrderId, string? Notes) : ICommand;
