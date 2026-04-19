using ZimMarket.Application.Common.Abstractions;

namespace ZimMarket.Application.Drivers;

public sealed record ConfirmBatchCollectedCommand(Guid BatchId) : ICommand;
