namespace ZimMarket.Infrastructure.ExchangeRates;

public interface IUsdZwlRateProvider
{
    Task<decimal?> GetUsdToZwlAsync(CancellationToken cancellationToken = default);
}
