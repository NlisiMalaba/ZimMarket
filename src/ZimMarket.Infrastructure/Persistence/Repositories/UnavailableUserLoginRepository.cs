using ZimMarket.Domain.Entities.Users;
using ZimMarket.Domain.Interfaces.Repositories;

namespace ZimMarket.Infrastructure.Persistence.Repositories;

/// <summary>
/// Fallback login repository used when persistence is not configured.
/// </summary>
internal sealed class UnavailableUserLoginRepository : IUserLoginRepository
{
    private static InvalidOperationException CreateUnavailableException() =>
        new("Database services are not configured. Set ConnectionStrings:DefaultConnection to enable authentication persistence.");

    public Task<User?> GetTrackedByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default) =>
        throw CreateUnavailableException();

    public Task<User?> GetTrackedByIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        throw CreateUnavailableException();

    public Task<User?> GetTrackedUserByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default) =>
        throw CreateUnavailableException();
}
