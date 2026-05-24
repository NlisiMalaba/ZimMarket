using MediatR;
using Microsoft.Extensions.Logging;
using ZimMarket.Application.Common.Models;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Domain.Enums;

namespace ZimMarket.Application.Files;

public sealed class GetPresignedUploadUrlQueryHandler : IRequestHandler<GetPresignedUploadUrlQuery, Result<PresignedUrlDto>>
{
    private static readonly TimeSpan PresignedUploadTtl = TimeSpan.FromHours(1);
    private const string ContainerProductImages = "product-images";
    private const string ContainerProfilePhotos = "profile-photos";
    private const string ContainerKycDocuments = "kyc-documents";
    private const string ContainerDeliveryPhotos = "delivery-photos";

    private readonly ICurrentUser _currentUser;
    private readonly IFileStorage _fileStorage;
    private readonly ILogger<GetPresignedUploadUrlQueryHandler> _logger;

    public GetPresignedUploadUrlQueryHandler(
        ICurrentUser currentUser,
        IFileStorage fileStorage,
        ILogger<GetPresignedUploadUrlQueryHandler> logger)
    {
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _fileStorage = fileStorage ?? throw new ArgumentNullException(nameof(fileStorage));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<PresignedUrlDto>> Handle(
        GetPresignedUploadUrlQuery request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId == Guid.Empty)
        {
            _logger.LogDebug("Presigned upload URL rejected: unauthenticated caller.");
            return Result<PresignedUrlDto>.Failure("Files.Forbidden", "Authentication is required.");
        }

        Result<PresignedUrlDto>? roleCheck = ValidateFileTypeForRole(request.FileType);
        if (roleCheck is not null)
        {
            return roleCheck;
        }

        string contentType = request.ContentType.Trim().ToLowerInvariant();
        string container = ResolveContainer(request.FileType);
        string extension = ResolveFileExtension(contentType);
        string key = $"{container}/{_currentUser.UserId:D}/{Guid.NewGuid():N}.{extension}";

        try
        {
            string uploadUrl = await _fileStorage
                .GetPresignedUploadUrlAsync(key, contentType, cancellationToken)
                .ConfigureAwait(false);

            return Result<PresignedUrlDto>.Success(new PresignedUrlDto
            {
                UploadUrl = uploadUrl,
                FileKey = key,
                ExpiresAt = DateTimeOffset.UtcNow.Add(PresignedUploadTtl)
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogDebug(ex, "Presigned upload URL rejected due to storage argument validation.");
            return Result<PresignedUrlDto>.ValidationFailure(
            [
                new ValidationError(nameof(GetPresignedUploadUrlQuery.ContentType), ex.Message)
            ]);
        }
    }

    private Result<PresignedUrlDto>? ValidateFileTypeForRole(FileType fileType)
    {
        if (fileType is FileType.NationalId or FileType.ProofOfResidence)
        {
            if (_currentUser.Role != UserRole.Seller)
            {
                _logger.LogDebug(
                    "Presigned upload URL rejected: file type {FileType} requires seller role.",
                    fileType);
                return Result<PresignedUrlDto>.Failure(
                    "Files.Forbidden",
                    "National ID and proof of residence uploads are only available to seller accounts.");
            }
        }

        if (fileType is FileType.DriverLicense or FileType.VehicleDoc)
        {
            if (_currentUser.Role != UserRole.Driver)
            {
                return Result<PresignedUrlDto>.Failure(
                    "Files.Forbidden",
                    "Driver license and vehicle document uploads are only available to driver accounts.");
            }
        }

        if (fileType == FileType.ProductImage && _currentUser.Role != UserRole.Seller)
        {
            return Result<PresignedUrlDto>.Failure(
                "Files.Forbidden",
                "Product image uploads are only available to seller accounts.");
        }

        if (fileType == FileType.ProfilePhoto && _currentUser.Role != UserRole.Seller)
        {
            return Result<PresignedUrlDto>.Failure(
                "Files.Forbidden",
                "Profile photo uploads are only available to seller accounts.");
        }

        return null;
    }

    private static string ResolveContainer(FileType fileType) =>
        fileType switch
        {
            FileType.ProductImage => ContainerProductImages,
            FileType.NationalId => ContainerKycDocuments,
            FileType.ProofOfResidence => ContainerKycDocuments,
            FileType.DriverLicense => ContainerKycDocuments,
            FileType.VehicleDoc => ContainerKycDocuments,
            FileType.DeliveryPhoto => ContainerDeliveryPhotos,
            FileType.ProfilePhoto => ContainerProfilePhotos,
            _ => throw new ArgumentOutOfRangeException(nameof(fileType), fileType, "Unsupported file type.")
        };

    private static string ResolveFileExtension(string contentType) =>
        contentType switch
        {
            "image/jpeg" => "jpg",
            "image/png" => "png",
            "image/webp" => "webp",
            _ => throw new ArgumentException($"Unsupported content type '{contentType}'.", nameof(contentType))
        };
}
