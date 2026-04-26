using ZimMarket.Domain.Interfaces.Repositories;
using ZimMarket.Domain.ValueObjects;

namespace ZimMarket.Infrastructure.Persistence.Repositories;

/// <summary>
/// Fallback identity-read repository used when persistence is not configured.
/// </summary>
internal sealed class UnavailableUserIdentityReadRepository : IUserIdentityReadRepository
{
    private static InvalidOperationException CreateUnavailableException() =>
        new("Database services are not configured. Set ConnectionStrings:DefaultConnection to enable authentication persistence.");

    public Task<bool> ExistsWithEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default) =>
        throw CreateUnavailableException();

    public Task<bool> ExistsWithPhoneAsync(PhoneNumber phoneNumber, CancellationToken cancellationToken = default) =>
        throw CreateUnavailableException();
}
