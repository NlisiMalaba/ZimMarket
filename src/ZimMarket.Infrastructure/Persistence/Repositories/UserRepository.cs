using Microsoft.EntityFrameworkCore;
using ZimMarket.Domain.Entities.Users;
using ZimMarket.Domain.Interfaces.Repositories;
using ZimMarket.Domain.ValueObjects;

namespace ZimMarket.Infrastructure.Persistence.Repositories;

internal sealed class UserRepository<TUser> : IUserRepository<TUser>
    where TUser : User
{
    private readonly AppDbContext _dbContext;
    private readonly DbSet<TUser> _users;

    public UserRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _users = _dbContext.Set<TUser>();
    }

    public Task<TUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<TUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        _users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Email == email, cancellationToken);

    public Task<TUser?> GetByPhoneAsync(PhoneNumber phoneNumber, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(phoneNumber);
        return _users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.PhoneNumber.Value == phoneNumber.Value, cancellationToken);
    }

    public async Task AddAsync(TUser user, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);
        await _users.AddAsync(user, cancellationToken);
    }

    public Task UpdateAsync(TUser user, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);
        _users.Update(user);
        return Task.CompletedTask;
    }
}
