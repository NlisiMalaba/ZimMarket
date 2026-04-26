using ZimMarket.Domain.Enums;

namespace ZimMarket.Domain.Entities.Users;

public sealed class AuthToken : BaseEntity
{
    private AuthToken()
    {
    }

    public AuthToken(
        Guid id,
        Guid userId,
        AuthTokenPurpose purpose,
        string tokenHash,
        DateTimeOffset expiresAt,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        Id = id;
        UserId = userId;
        Purpose = purpose;
        TokenHash = tokenHash;
        ExpiresAt = expiresAt;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public Guid UserId { get; private set; }

    public AuthTokenPurpose Purpose { get; private set; }

    public string TokenHash { get; private set; } = null!;

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? ConsumedAt { get; private set; }

    public bool IsConsumed => ConsumedAt.HasValue;

    public bool IsExpired(DateTimeOffset nowUtc) => ExpiresAt <= nowUtc;

    public void MarkConsumed(DateTimeOffset consumedAtUtc)
    {
        if (IsConsumed)
            return;

        ConsumedAt = consumedAtUtc;
        UpdatedAt = consumedAtUtc;
    }
}
