using ZimMarket.Application.Common.Abstractions;

namespace ZimMarket.Application.Logistics;

public sealed record GetBatchDetailsQuery(Guid BatchId) : IQuery<DeliveryBatchDetailDto>;
