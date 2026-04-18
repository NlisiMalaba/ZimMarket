using ZimMarket.Domain.Entities.Payments;

namespace ZimMarket.Domain.Interfaces.Repositories;

public interface IPaymentIdempotencyRepository
{
    Task<PaymentIdempotencyRecord?> GetByKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default);

    Task AddAsync(PaymentIdempotencyRecord record, CancellationToken cancellationToken = default);
}
