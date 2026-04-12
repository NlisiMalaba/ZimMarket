using ZimMarket.Domain.Enums;
using ZimMarket.Domain.ValueObjects;

namespace ZimMarket.Domain.Entities.Users;

public abstract class User : BaseEntity
{
    protected User()
    {
    }

    protected User(
        Guid id,
        string email,
        PhoneNumber phoneNumber,
        string passwordHash,
        UserRole role,
        KycStatus kycStatus,
        bool isActive,
        string? refreshTokenHash,
        DateTimeOffset? refreshTokenExpiry,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        Id = id;
        Email = email;
        PhoneNumber = phoneNumber;
        PasswordHash = passwordHash;
        Role = role;
        KycStatus = kycStatus;
        IsActive = isActive;
        RefreshTokenHash = refreshTokenHash;
        RefreshTokenExpiry = refreshTokenExpiry;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public string Email { get; private set; } = null!;

    public PhoneNumber PhoneNumber { get; private set; } = null!;

    public string PasswordHash { get; private set; } = null!;

    public UserRole Role { get; private set; }

    public KycStatus KycStatus { get; private set; }

    public bool IsActive { get; private set; }

    public string? RefreshTokenHash { get; private set; }

    public DateTimeOffset? RefreshTokenExpiry { get; private set; }

    public void Deactivate()
    {
        if (!IsActive)
            return;

        IsActive = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    protected void SetKycStatus(KycStatus kycStatus)
    {
        KycStatus = kycStatus;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
