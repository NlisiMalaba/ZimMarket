using ZimMarket.Application.Common.Abstractions;

namespace ZimMarket.Application.Sellers;

public sealed record GetSellerVerificationQuery : IQuery<SellerVerificationDto>;
