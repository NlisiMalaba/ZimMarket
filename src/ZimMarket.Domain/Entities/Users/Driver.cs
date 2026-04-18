using ZimMarket.Domain.Enums;
using ZimMarket.Domain.Events;
using ZimMarket.Domain.Exceptions;
using ZimMarket.Domain.ValueObjects;

namespace ZimMarket.Domain.Entities.Users;

public sealed class Driver : User
{
    private Driver()
    {
    }

    public Driver(
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
        string licenseNumber,
        string licenseDocumentKey,
        string vehicleRegistration,
        string vehicleDocumentKey,
        DriverStatus driverStatus,
        GeoCoordinate? lastKnownLocation,
        bool isApproved,
        string? rejectionReason)
        : base(
            id,
            email,
            fullName,
            phoneNumber,
            passwordHash,
            UserRole.Driver,
            kycStatus,
            isActive,
            refreshTokenHash,
            refreshTokenExpiry,
            createdAt,
            updatedAt)
    {
        LicenseNumber = licenseNumber;
        LicenseDocumentKey = licenseDocumentKey;
        VehicleRegistration = vehicleRegistration;
        VehicleDocumentKey = vehicleDocumentKey;
        DriverStatus = driverStatus;
        LastKnownLocation = lastKnownLocation;
        IsApproved = isApproved;
        RejectionReason = rejectionReason;
    }

    public string LicenseNumber { get; private set; } = string.Empty;

    public string LicenseDocumentKey { get; private set; } = string.Empty;

    public string VehicleRegistration { get; private set; } = string.Empty;

    public string VehicleDocumentKey { get; private set; } = string.Empty;

    public DriverStatus DriverStatus { get; private set; }

    public GeoCoordinate? LastKnownLocation { get; private set; }

    public bool IsApproved { get; private set; }

    public string? RejectionReason { get; private set; }

    public void Approve()
    {
        if (KycStatus != KycStatus.PendingReview)
            throw new DomainException("Driver KYC can only be approved while pending review.");

        IsApproved = true;
        RejectionReason = null;
        SetKycStatus(KycStatus.Approved);
        AddDomainEvent(new DriverApprovedEvent(Id));
    }

    public void Reject(string reason)
    {
        if (KycStatus != KycStatus.PendingReview)
            throw new DomainException("Driver KYC can only be rejected while pending review.");

        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("Rejection reason is required.");

        var trimmed = reason.Trim();
        IsApproved = false;
        RejectionReason = trimmed;
        SetKycStatus(KycStatus.Rejected);
        AddDomainEvent(new DriverRejectedEvent(Id, trimmed));
    }

    public void SubmitKyc(
        string licenseNumber,
        string licenseDocumentKey,
        string vehicleRegistration,
        string vehicleDocumentKey)
    {
        if (KycStatus != KycStatus.NotSubmitted)
            throw new DomainException("KYC documents can only be submitted when KYC has not yet been submitted.");

        if (string.IsNullOrWhiteSpace(licenseNumber))
            throw new DomainException("License number is required.");

        if (string.IsNullOrWhiteSpace(licenseDocumentKey))
            throw new DomainException("License document key is required.");

        if (string.IsNullOrWhiteSpace(vehicleRegistration))
            throw new DomainException("Vehicle registration is required.");

        if (string.IsNullOrWhiteSpace(vehicleDocumentKey))
            throw new DomainException("Vehicle document key is required.");

        LicenseNumber = licenseNumber.Trim();
        LicenseDocumentKey = licenseDocumentKey.Trim();
        VehicleRegistration = vehicleRegistration.Trim();
        VehicleDocumentKey = vehicleDocumentKey.Trim();
        SetKycStatus(KycStatus.PendingReview);
    }

    public void UpdateLocation(GeoCoordinate coordinate, IReadOnlyList<Guid>? activeOrderIds = null)
    {
        ArgumentNullException.ThrowIfNull(coordinate);

        LastKnownLocation = coordinate;
        UpdatedAt = DateTimeOffset.UtcNow;

        var orders = activeOrderIds is null
            ? new List<Guid>()
            : new List<Guid>(activeOrderIds);
        AddDomainEvent(new DriverLocationUpdatedEvent(Id, coordinate.Latitude, coordinate.Longitude, orders));
    }

    public void SetStatus(DriverStatus status)
    {
        DriverStatus = status;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
