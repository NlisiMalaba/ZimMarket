using Microsoft.EntityFrameworkCore;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Domain.Entities.Users;
using ZimMarket.Domain.Interfaces.Repositories;

namespace ZimMarket.Infrastructure.Persistence.Repositories;

internal sealed class UserLoginRepository : IUserLoginRepository
{
    private readonly AppDbContext _dbContext;
    private readonly IJwtService _jwtService;

    public UserLoginRepository(AppDbContext dbContext, IJwtService jwtService)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _jwtService = jwtService ?? throw new ArgumentNullException(nameof(jwtService));
    }

    public Task<User?> GetTrackedByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(normalizedEmail);

        return _dbContext.Set<User>()
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);
    }

    public Task<User?> GetTrackedByIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        _dbContext.Set<User>().FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

    public async Task<User?> GetTrackedUserByRefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return null;

        DateTimeOffset now = DateTimeOffset.UtcNow;
        List<User> candidates = await _dbContext.Set<User>()
            .AsTracking()
            .Where(u => u.RefreshTokenHash != null && u.RefreshTokenExpiry > now)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (User user in candidates)
        {
            if (_jwtService.VerifyRefreshToken(refreshToken, user.RefreshTokenHash!))
                return user;
        }

        return null;
    }
}
