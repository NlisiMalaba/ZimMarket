using ZimMarket.Domain.Enums;

namespace ZimMarket.Domain.ReadModels;

/// <summary>Admin queue row for users awaiting KYC review (seller or driver).</summary>
public sealed record PendingKycQueueRow(
    Guid UserId,
    string Email,
    string FullName,
    UserRole Role,
    string? BusinessName,
    string? LicenseNumber,
    string? VehicleRegistration,
    string? NationalIdDocumentKey,
    string? ProofOfResidenceDocumentKey,
    string? LicenseDocumentKey,
    string? VehicleDocumentKey);
