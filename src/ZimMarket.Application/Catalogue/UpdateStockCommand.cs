using ZimMarket.Application.Common.Abstractions;

namespace ZimMarket.Application.Catalogue;

public sealed record UpdateStockCommand(Guid ProductId, int Delta) : ICommand;
