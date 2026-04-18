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
        string fullName,
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
        FullName = fullName;
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

    public string FullName { get; private set; } = null!;

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

    /// <summary>
    /// Persists a PBKDF2 hash of the refresh token (never the raw token). Call <see cref="ClearRefreshToken"/> on logout or rotation.
    /// </summary>
    public void SetRefreshToken(string pbkdf2StoredHash, DateTimeOffset expiresAtUtc)
    {
        if (string.IsNullOrWhiteSpace(pbkdf2StoredHash))
            throw new ArgumentException("Refresh token hash is required.", nameof(pbkdf2StoredHash));

        RefreshTokenHash = pbkdf2StoredHash.Trim();
        RefreshTokenExpiry = expiresAtUtc;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void ClearRefreshToken()
    {
        RefreshTokenHash = null;
        RefreshTokenExpiry = null;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    protected void SetKycStatus(KycStatus kycStatus)
    {
        KycStatus = kycStatus;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
