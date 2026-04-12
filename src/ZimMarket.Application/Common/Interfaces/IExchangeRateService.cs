namespace ZimMarket.Application.Common.Interfaces;

public interface IExchangeRateService
{
    Task<decimal> GetUsdToZwlAsync(CancellationToken cancellationToken = default);
}
