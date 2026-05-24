using MediatR;
using Microsoft.Extensions.Logging;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Application.Common.Models;
using ZimMarket.Domain.Entities.Users;
using ZimMarket.Domain.Enums;
using ZimMarket.Domain.Interfaces;
using ZimMarket.Domain.Interfaces.Repositories;

namespace ZimMarket.Application.Files;

public sealed class UploadProfilePhotoCommandHandler : IRequestHandler<UploadProfilePhotoCommand, Result<string>>
{
    private readonly ICurrentUser _currentUser;
    private readonly IUserLoginRepository _userLoginRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileStorage _fileStorage;
    private readonly ILogger<UploadProfilePhotoCommandHandler> _logger;

    public UploadProfilePhotoCommandHandler(
        ICurrentUser currentUser,
        IUserLoginRepository userLoginRepository,
        IUnitOfWork unitOfWork,
        IFileStorage fileStorage,
        ILogger<UploadProfilePhotoCommandHandler> logger)
    {
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _userLoginRepository = userLoginRepository ?? throw new ArgumentNullException(nameof(userLoginRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _fileStorage = fileStorage ?? throw new ArgumentNullException(nameof(fileStorage));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<string>> Handle(UploadProfilePhotoCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId == Guid.Empty)
        {
            return Result<string>.Failure("Files.Forbidden", "Authentication is required.");
        }

        if (_currentUser.Role != UserRole.Seller)
        {
            return Result<string>.Failure("Files.Forbidden", "Only sellers can upload profile photos.");
        }

        User? trackedUser = await _userLoginRepository
            .GetTrackedByIdAsync(_currentUser.UserId, cancellationToken)
            .ConfigureAwait(false);

        if (trackedUser is not Seller seller)
        {
            return Result<string>.Failure("Seller.NotFound", "Seller profile was not found.");
        }

        string? previousProfilePhotoKey = seller.ProfilePhotoKey;

        string contentType = request.ContentType.Trim().ToLowerInvariant();
        string extension = contentType switch
        {
            "image/jpeg" => "jpg",
            "image/png" => "png",
            "image/webp" => "webp",
            _ => throw new InvalidOperationException($"Unsupported content type '{contentType}'.")
        };

        string key = ProfilePhotoStorage.BuildKey(_currentUser.UserId, extension);

        try
        {
            string storedKey = await _fileStorage
                .UploadAsync(request.Content, key, contentType, cancellationToken)
                .ConfigureAwait(false);

            seller.SetProfilePhotoKey(storedKey);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            if (!string.IsNullOrWhiteSpace(previousProfilePhotoKey) &&
                !string.Equals(previousProfilePhotoKey.Trim(), storedKey, StringComparison.Ordinal))
            {
                await ProfilePhotoStorage
                    .TryDeleteAsync(_fileStorage, _logger, seller.Id, previousProfilePhotoKey, cancellationToken)
                    .ConfigureAwait(false);
            }

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
