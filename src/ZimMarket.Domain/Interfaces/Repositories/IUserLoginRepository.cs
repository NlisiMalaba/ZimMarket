using ZimMarket.Domain.Entities.Users;

namespace ZimMarket.Domain.Interfaces.Repositories;

/// <summary>
/// Loads a user for authentication flows (tracked entity so refresh token updates persist).
/// </summary>
public interface IUserLoginRepository
{
    Task<User?> GetTrackedByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default);

    Task<User?> GetTrackedByIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves a tracked user whose stored refresh token matches <paramref name="refreshToken"/> (PBKDF2 verify).
    /// </summary>
    Task<User?> GetTrackedUserByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
}
