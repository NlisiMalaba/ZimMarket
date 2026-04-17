using ZimMarket.Domain.Enums;
using ZimMarket.Domain.ValueObjects;

namespace ZimMarket.Domain.Entities.Users;

/// <summary>
/// Platform administrator (TPH discriminator <see cref="UserRole.Admin"/>).
/// </summary>
public sealed class AdminUser : User
{
    private AdminUser()
    {
    }

    public AdminUser(
        Guid id,
        string email,
        PhoneNumber phoneNumber,
        string passwordHash,
        KycStatus kycStatus,
        bool isActive,
        string? refreshTokenHash,
        DateTimeOffset? refreshTokenExpiry,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
        : base(
            id,
            email,
            phoneNumber,
            passwordHash,
            UserRole.Admin,
            kycStatus,
            isActive,
            refreshTokenHash,
            refreshTokenExpiry,
            createdAt,
            updatedAt)
    {
    }
}
