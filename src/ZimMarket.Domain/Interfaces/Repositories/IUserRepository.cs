using ZimMarket.Domain.Entities.Users;
using ZimMarket.Domain.ValueObjects;

namespace ZimMarket.Domain.Interfaces.Repositories;

public interface IUserRepository<T>
    where T : User
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<T?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<T?> GetByPhoneAsync(PhoneNumber phoneNumber, CancellationToken cancellationToken = default);

    Task AddAsync(T user, CancellationToken cancellationToken = default);

    Task UpdateAsync(T user, CancellationToken cancellationToken = default);
}
