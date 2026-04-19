using ZimMarket.Application.Common.Abstractions;

namespace ZimMarket.Application.Logistics;

public sealed record CreateDeliveryBatchCommand(
    IReadOnlyList<Guid> OrderIds,
    Guid DriverId) : ICommand<Guid>;
