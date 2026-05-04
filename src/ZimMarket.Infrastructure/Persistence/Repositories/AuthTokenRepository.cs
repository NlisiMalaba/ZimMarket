using Microsoft.EntityFrameworkCore;
using ZimMarket.Domain.Entities.Users;
using ZimMarket.Domain.Enums;
using ZimMarket.Domain.Interfaces.Repositories;

namespace ZimMarket.Infrastructure.Persistence.Repositories;

internal sealed class AuthTokenRepository : IAuthTokenRepository
{
    private readonly AppDbContext _dbContext;

    public AuthTokenRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public Task AddAsync(AuthToken token, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(token);
        return _dbContext.Set<AuthToken>().AddAsync(token, cancellationToken).AsTask();
    }

    public Task<AuthToken?> GetActiveByHashAsync(
        string tokenHash,
        AuthTokenPurpose purpose,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);
        DateTimeOffset now = DateTimeOffset.UtcNow;

        return _dbContext.Set<AuthToken>()
            .FirstOrDefaultAsync(
                t => t.TokenHash == tokenHash
                    && t.Purpose == purpose
                    && t.ConsumedAt == null
                    && t.ExpiresAt > now,
                cancellationToken);
    }

    public Task<int> RevokeActiveTokensAsync(
        Guid userId,
        AuthTokenPurpose purpose,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Set<AuthToken>()
            .Where(t => t.UserId == userId && t.Purpose == purpose && t.ConsumedAt == null && t.ExpiresAt > nowUtc)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(t => t.ConsumedAt, nowUtc)
                    .SetProperty(t => t.UpdatedAt, nowUtc),
                cancellationToken);
    }
}
