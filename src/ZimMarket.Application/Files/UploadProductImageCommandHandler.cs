using MediatR;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Application.Common.Models;
using ZimMarket.Domain.Enums;

namespace ZimMarket.Application.Files;

public sealed class UploadProductImageCommandHandler : IRequestHandler<UploadProductImageCommand, Result<string>>
{
    private const string ContainerProductImages = "product-images";

    private readonly ICurrentUser _currentUser;
    private readonly IFileStorage _fileStorage;

    public UploadProductImageCommandHandler(ICurrentUser currentUser, IFileStorage fileStorage)
    {
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _fileStorage = fileStorage ?? throw new ArgumentNullException(nameof(fileStorage));
    }

    public async Task<Result<string>> Handle(UploadProductImageCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId == Guid.Empty)
        {
            return Result<string>.Failure("Files.Forbidden", "Authentication is required.");
        }

        if (_currentUser.Role != UserRole.Seller)
        {
            return Result<string>.Failure("Files.Forbidden", "Only sellers can upload product images.");
        }

        string contentType = request.ContentType.Trim().ToLowerInvariant();
        string extension = contentType switch
        {
            "image/jpeg" => "jpg",
            "image/png" => "png",
            "image/webp" => "webp",
            _ => throw new InvalidOperationException($"Unsupported content type '{contentType}'.")
        };

        string key = $"{ContainerProductImages}/{_currentUser.UserId:D}/{Guid.NewGuid():N}.{extension}";

        try
        {
            string storedKey = await _fileStorage
                .UploadAsync(request.Content, key, contentType, cancellationToken)
                .ConfigureAwait(false);

            return Result<string>.Success(storedKey);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not configured", StringComparison.OrdinalIgnoreCase))
        {
            return Result<string>.Failure("Files.StorageUnavailable", ex.Message);
        }
        catch (ArgumentException ex)
        {
            return Result<string>.ValidationFailure(
            [
                new ValidationError(nameof(request.ContentType), ex.Message)
            ]);
        }
    }
}
