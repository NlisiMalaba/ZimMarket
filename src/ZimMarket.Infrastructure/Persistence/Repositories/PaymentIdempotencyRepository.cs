using Microsoft.EntityFrameworkCore;
using ZimMarket.Domain.Entities.Payments;
using ZimMarket.Domain.Interfaces.Repositories;

namespace ZimMarket.Infrastructure.Persistence.Repositories;

internal sealed class PaymentIdempotencyRepository : IPaymentIdempotencyRepository
{
    private readonly AppDbContext _dbContext;

    public PaymentIdempotencyRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public Task<PaymentIdempotencyRecord?> GetByKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default) =>
        _dbContext.PaymentIdempotencyRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.IdempotencyKey == idempotencyKey, cancellationToken);

    public async Task AddAsync(PaymentIdempotencyRecord record, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);
        await _dbContext.PaymentIdempotencyRecords.AddAsync(record, cancellationToken);
    }
}
