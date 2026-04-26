namespace ZimMarket.Domain.Entities.Users;

public sealed class AdminApprovalState : BaseEntity
{
    private AdminApprovalState()
    {
    }

    public AdminApprovalState(
        Guid userId,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        Id = userId;
        UserId = userId;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public Guid UserId { get; private set; }

    public DateTimeOffset? EmailVerifiedAt { get; private set; }

    public DateTimeOffset? ApprovedAt { get; private set; }

    public Guid? ApprovedByUserId { get; private set; }

    public bool IsEmailVerified => EmailVerifiedAt.HasValue;

    public bool IsApproved => ApprovedAt.HasValue;

    public void MarkEmailVerified(DateTimeOffset verifiedAtUtc)
    {
        if (IsEmailVerified)
            return;

        EmailVerifiedAt = verifiedAtUtc;
        UpdatedAt = verifiedAtUtc;
    }

    public void MarkApproved(Guid approvedByUserId, DateTimeOffset approvedAtUtc)
    {
        ApprovedByUserId = approvedByUserId;
        ApprovedAt = approvedAtUtc;
        UpdatedAt = approvedAtUtc;
    }
}
