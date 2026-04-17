using ZimMarket.Domain.Enums;
using ZimMarket.Domain.ValueObjects;

namespace ZimMarket.Domain.Entities.Users;

/// <summary>
/// Highest-privilege platform user (TPH discriminator <see cref="UserRole.SuperAdmin"/>).
/// </summary>
public sealed class SuperAdminUser : User
{
    private SuperAdminUser()
    {
    }

    public SuperAdminUser(
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
            UserRole.SuperAdmin,
            kycStatus,
            isActive,
            refreshTokenHash,
            refreshTokenExpiry,
            createdAt,
            updatedAt)
    {
    }
}
