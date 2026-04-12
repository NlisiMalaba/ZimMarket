namespace ZimMarket.Domain.Common;

public sealed record ProductFilter(
    string? SearchTerm,
    Guid? CategoryId,
    decimal? MinPriceUsd,
    decimal? MaxPriceUsd);
