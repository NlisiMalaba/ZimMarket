using ZimMarket.Application.Common.Abstractions;
using ZimMarket.Application.Common.Models;

namespace ZimMarket.Application.Catalogue;

public sealed record GetSellerProductDetailQuery(Guid ProductId) : IQuery<SellerProductDetailDto>;
