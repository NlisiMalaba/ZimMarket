using ZimMarket.Domain.Enums;
using ZimMarket.Domain.Events;
using ZimMarket.Domain.Exceptions;
using ZimMarket.Domain.ValueObjects;

namespace ZimMarket.Domain.Entities.Users;

public sealed class Seller : User
{
    private Seller()
    {
    }

    public Seller(
        Guid id,
        string email,
        string fullName,
        PhoneNumber phoneNumber,
        string passwordHash,
        KycStatus kycStatus,
        bool isActive,
        string? refreshTokenHash,
        DateTimeOffset? refreshTokenExpiry,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt,
        string businessName,
        string nationalIdDocumentKey,
        string proofOfResidenceDocumentKey,
        bool isApproved,
        string? rejectionReason)
        : base(
            id,
            email,
            fullName,
            phoneNumber,
            passwordHash,
            UserRole.Seller,
            kycStatus,
            isActive,
            refreshTokenHash,
            refreshTokenExpiry,
            createdAt,
            updatedAt)
    {
        BusinessName = businessName;
        NationalIdDocumentKey = nationalIdDocumentKey;
        ProofOfResidenceDocumentKey = proofOfResidenceDocumentKey;
        IsApproved = isApproved;
        RejectionReason = rejectionReason;
    }

    public string BusinessName { get; private set; } = null!;

    public string NationalIdDocumentKey { get; private set; } = string.Empty;

    public string ProofOfResidenceDocumentKey { get; private set; } = string.Empty;

    public bool IsApproved { get; private set; }

    public string? RejectionReason { get; private set; }

    public void Approve()
    {
        if (KycStatus != KycStatus.PendingReview)
            throw new DomainException("Seller KYC can only be approved while pending review.");

        IsApproved = true;
        RejectionReason = null;
        SetKycStatus(KycStatus.Approved);
        AddDomainEvent(new SellerApprovedEvent(Id));
    }

    public void Reject(string reason)
    {
        if (KycStatus != KycStatus.PendingReview)
            throw new DomainException("Seller KYC can only be rejected while pending review.");

        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("Rejection reason is required.");

        var trimmed = reason.Trim();
        IsApproved = false;
        RejectionReason = trimmed;
        SetKycStatus(KycStatus.Rejected);
        AddDomainEvent(new SellerRejectedEvent(Id, trimmed));
    }

    public void SubmitKyc(string nationalIdKey, string proofKey)
    {
        if (KycStatus != KycStatus.NotSubmitted)
            throw new DomainException("KYC documents can only be submitted when KYC has not yet been submitted.");

        if (string.IsNullOrWhiteSpace(nationalIdKey))
            throw new DomainException("National ID document key is required.");

        if (string.IsNullOrWhiteSpace(proofKey))
            throw new DomainException("Proof of residence document key is required.");

        NationalIdDocumentKey = nationalIdKey.Trim();
        ProofOfResidenceDocumentKey = proofKey.Trim();
        SetKycStatus(KycStatus.PendingReview);
    }
}
