using ZimMarket.Application.Common.Abstractions;

namespace ZimMarket.Application.Drivers;

public sealed record GetActiveDriverLocationsQuery : IQuery<IReadOnlyList<ActiveDriverLocationDto>>;
