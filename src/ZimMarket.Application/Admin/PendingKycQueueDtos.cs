using ZimMarket.Domain.Enums;

namespace ZimMarket.Application.Admin;

public sealed record KycDocumentSasDto(string StorageKey, string Url, DateTimeOffset ExpiresAt);

public sealed record PendingKycQueueItemDto(
    Guid UserId,
    string Email,
    string FullName,
    UserRole Role,
    string? BusinessName,
    string? LicenseNumber,
    string? VehicleRegistration,
    KycDocumentSasDto? NationalId,
    KycDocumentSasDto? ProofOfResidence,
    KycDocumentSasDto? LicenseDocument,
    KycDocumentSasDto? VehicleDocument);
