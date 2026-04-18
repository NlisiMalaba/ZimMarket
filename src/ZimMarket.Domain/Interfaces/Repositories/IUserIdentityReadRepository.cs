using ZimMarket.Domain.ValueObjects;

namespace ZimMarket.Domain.Interfaces.Repositories;

/// <summary>
/// Read-side checks across all persisted users (TPH), for registration and auth flows.
/// </summary>
public interface IUserIdentityReadRepository
{
    Task<bool> ExistsWithEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default);

    Task<bool> ExistsWithPhoneAsync(PhoneNumber phoneNumber, CancellationToken cancellationToken = default);
}
