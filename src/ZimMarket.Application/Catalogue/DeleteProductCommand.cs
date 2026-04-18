using ZimMarket.Application.Common.Abstractions;

namespace ZimMarket.Application.Catalogue;

public sealed record DeleteProductCommand(Guid ProductId) : ICommand;
