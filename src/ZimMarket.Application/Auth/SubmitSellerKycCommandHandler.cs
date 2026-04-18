using MediatR;
using Microsoft.Extensions.Logging;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Application.Common.Models;
using ZimMarket.Domain.Entities.Users;
using ZimMarket.Domain.Enums;
using ZimMarket.Domain.Exceptions;
using ZimMarket.Domain.Interfaces;

namespace ZimMarket.Application.Auth;

public sealed class SubmitSellerKycCommandHandler : IRequestHandler<SubmitSellerKycCommand, Result>
{
    private readonly ICurrentUser _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileStorage _fileStorage;
    private readonly ILogger<SubmitSellerKycCommandHandler> _logger;

    public SubmitSellerKycCommandHandler(
        ICurrentUser currentUser,
        IUnitOfWork unitOfWork,
        IFileStorage fileStorage,
        ILogger<SubmitSellerKycCommandHandler> logger)
    {
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _fileStorage = fileStorage ?? throw new ArgumentNullException(nameof(fileStorage));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result> Handle(SubmitSellerKycCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.Role != UserRole.Seller)
        {
            _logger.LogDebug("Seller KYC submit rejected: caller is not an authenticated seller.");
            return Result.Failure("Kyc.Forbidden", "Only authenticated sellers can submit KYC documents.");
        }

        string nationalIdKey = request.NationalIdKey.Trim();
        string proofKey = request.ProofOfResidenceKey.Trim();

        Seller? seller = await _unitOfWork.Sellers
            .GetByIdAsync(_currentUser.UserId, cancellationToken)
            .ConfigureAwait(false);

        if (seller is null)
        {
            _logger.LogWarning("Seller KYC submit failed: no seller record for user {UserId}.", _currentUser.UserId);
            return Result.Failure("Kyc.SellerNotFound", "Seller profile was not found.");
        }

        Result? nationalIdCheck = await CheckBlobExistsAsync(
                nationalIdKey,
                nameof(SubmitSellerKycCommand.NationalIdKey),
                "The national ID document was not found in storage. Upload the file first.",
                cancellationToken)
            .ConfigureAwait(false);

        if (nationalIdCheck is not null)
        {
            _logger.LogDebug("Seller KYC submit rejected: national ID key invalid or missing for seller {SellerId}.", seller.Id);
            return nationalIdCheck;
        }

        Result? proofCheck = await CheckBlobExistsAsync(
                proofKey,
                nameof(SubmitSellerKycCommand.ProofOfResidenceKey),
                "The proof of residence document was not found in storage. Upload the file first.",
                cancellationToken)
            .ConfigureAwait(false);

        if (proofCheck is not null)
        {
            _logger.LogDebug("Seller KYC submit rejected: proof of residence key invalid or missing for seller {SellerId}.", seller.Id);
            return proofCheck;
        }

        try
        {
            seller.SubmitKyc(nationalIdKey, proofKey);
        }
        catch (DomainException ex)
        {
            _logger.LogDebug(ex, "Seller KYC submit rejected by domain rules for seller {SellerId}.", seller.Id);
            return Result.Failure("Kyc.AlreadySubmitted", ex.Message);
        }

        await _unitOfWork.Sellers.UpdateAsync(seller, cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }

    private async Task<Result?> CheckBlobExistsAsync(
        string key,
        string fieldName,
        string notFoundMessage,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!await _fileStorage.ExistsAsync(key, cancellationToken).ConfigureAwait(false))
            {
                return Result.ValidationFailure([new ValidationError(fieldName, notFoundMessage)]);
            }
        }
        catch (ArgumentException ex)
        {
            return Result.ValidationFailure([new ValidationError(fieldName, ex.Message)]);
        }

        return null;
    }
}
