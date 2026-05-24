using Microsoft.EntityFrameworkCore;
using ZimMarket.Domain.Entities.Users;
using ZimMarket.Domain.Interfaces.Repositories;
using ZimMarket.Domain.ValueObjects;

namespace ZimMarket.Infrastructure.Persistence.Repositories;

internal sealed class UserIdentityReadRepository : IUserIdentityReadRepository
{
    private readonly AppDbContext _dbContext;

    public UserIdentityReadRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public Task<bool> ExistsWithEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(normalizedEmail);

        return _dbContext.Set<User>()
            .AnyAsync(u => u.Email.ToLower() == normalizedEmail, cancellationToken);
    }

    public Task<bool> ExistsWithPhoneAsync(PhoneNumber phoneNumber, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(phoneNumber);

        return _dbContext.Set<User>()
            .AnyAsync(u => u.PhoneNumber.Value == phoneNumber.Value, cancellationToken);
    }

    public Task<bool> ExistsWithEmailForOtherUserAsync(
        string normalizedEmail,
        Guid excludeUserId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(normalizedEmail);

        return _dbContext.Set<User>()
            .AnyAsync(
                u => u.Id != excludeUserId && u.Email.ToLower() == normalizedEmail,
                cancellationToken);
    }

    public Task<bool> ExistsWithPhoneForOtherUserAsync(
        PhoneNumber phoneNumber,
        Guid excludeUserId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(phoneNumber);

        return _dbContext.Set<User>()
            .AnyAsync(
                u => u.Id != excludeUserId && u.PhoneNumber.Value == phoneNumber.Value,
                cancellationToken);
    }
}
