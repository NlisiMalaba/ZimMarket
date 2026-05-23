using ZimMarket.Application.Common.Abstractions;

namespace ZimMarket.Application.Files;

public sealed record GetSellerProductImageQuery(string ImageKey) : IQuery<SellerProductImageContentDto>;

public sealed record SellerProductImageContentDto(string ContentType, Stream Content);
