using ZimMarket.Domain.Entities.Users;
using ZimMarket.Domain.Enums;

namespace ZimMarket.Domain.Interfaces.Repositories;

public interface IAuthTokenRepository
{
    Task AddAsync(AuthToken token, CancellationToken cancellationToken = default);

    Task<AuthToken?> GetActiveByHashAsync(
        string tokenHash,
        AuthTokenPurpose purpose,
        CancellationToken cancellationToken = default);

    Task<int> RevokeActiveTokensAsync(
        Guid userId,
        AuthTokenPurpose purpose,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken = default);
}
