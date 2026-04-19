using MediatR;
using Microsoft.Extensions.Logging;
using ZimMarket.Application.Common;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Application.Common.Models;
using ZimMarket.Domain.Enums;
using ZimMarket.Domain.Interfaces;
using ZimMarket.Domain.ReadModels;

namespace ZimMarket.Application.Admin;

public sealed class GetPendingKycQueryHandler
    : IRequestHandler<GetPendingKycQuery, Result<ZimMarket.Shared.PagedList<PendingKycQueueItemDto>>>
{
    private static readonly TimeSpan KycReadSasRequestedLifetime = TimeSpan.FromHours(1);

    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IFileStorage _fileStorage;
    private readonly ILogger<GetPendingKycQueryHandler> _logger;

    public GetPendingKycQueryHandler(
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IFileStorage fileStorage,
        ILogger<GetPendingKycQueryHandler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _fileStorage = fileStorage ?? throw new ArgumentNullException(nameof(fileStorage));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<ZimMarket.Shared.PagedList<PendingKycQueueItemDto>>> Handle(
        GetPendingKycQuery request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated
            || _currentUser.UserId == Guid.Empty
            || _currentUser.Role != UserRole.Admin)
        {
            _logger.LogDebug("Get pending KYC rejected: caller is not an admin.");
            return Result<ZimMarket.Shared.PagedList<PendingKycQueueItemDto>>.Failure(
                AdminKycErrorCodes.Forbidden,
                "Only administrators can list pending KYC submissions.");
        }

        var pagination = new ZimMarket.Shared.PaginationParams
        {
            Page = request.Page,
            PageSize = request.PageSize
        };

        ZimMarket.Shared.PagedList<PendingKycQueueRow> page = await _unitOfWork.PendingKyc
            .GetPagedPendingReviewAsync(request.Role, pagination, cancellationToken)
            .ConfigureAwait(false);

        var dtos = new List<PendingKycQueueItemDto>(page.Items.Count);
        foreach (PendingKycQueueRow row in page.Items)
        {
            Result<PendingKycQueueItemDto> mapped = await MapRowAsync(row, cancellationToken).ConfigureAwait(false);
            if (!mapped.IsSuccess)
                return Result<ZimMarket.Shared.PagedList<PendingKycQueueItemDto>>.Failure(mapped.ErrorCode!, mapped.ErrorMessage!);

            dtos.Add(mapped.Value!);
        }

        return Result<ZimMarket.Shared.PagedList<PendingKycQueueItemDto>>.Success(
            new ZimMarket.Shared.PagedList<PendingKycQueueItemDto>(dtos, page.Page, page.PageSize, page.TotalCount));
    }

    private async Task<Result<PendingKycQueueItemDto>> MapRowAsync(
        PendingKycQueueRow row,
        CancellationToken cancellationToken)
    {
        DateTimeOffset expiresAt = DateTimeOffset.UtcNow.Add(KycReadSasRequestedLifetime);

        Result<KycDocumentSasDto?> nationalId = await TrySasAsync(row.NationalIdDocumentKey, expiresAt, cancellationToken)
            .ConfigureAwait(false);
        if (!nationalId.IsSuccess)
            return Result<PendingKycQueueItemDto>.Failure(nationalId.ErrorCode!, nationalId.ErrorMessage!);

        Result<KycDocumentSasDto?> proof = await TrySasAsync(row.ProofOfResidenceDocumentKey, expiresAt, cancellationToken)
            .ConfigureAwait(false);
        if (!proof.IsSuccess)
            return Result<PendingKycQueueItemDto>.Failure(proof.ErrorCode!, proof.ErrorMessage!);

        Result<KycDocumentSasDto?> license = await TrySasAsync(row.LicenseDocumentKey, expiresAt, cancellationToken)
            .ConfigureAwait(false);
        if (!license.IsSuccess)
            return Result<PendingKycQueueItemDto>.Failure(license.ErrorCode!, license.ErrorMessage!);

        Result<KycDocumentSasDto?> vehicle = await TrySasAsync(row.VehicleDocumentKey, expiresAt, cancellationToken)
            .ConfigureAwait(false);
        if (!vehicle.IsSuccess)
            return Result<PendingKycQueueItemDto>.Failure(vehicle.ErrorCode!, vehicle.ErrorMessage!);

        return Result<PendingKycQueueItemDto>.Success(
            new PendingKycQueueItemDto(
                row.UserId,
                row.Email,
                row.FullName,
                row.Role,
                row.BusinessName,
                row.LicenseNumber,
                row.VehicleRegistration,
                nationalId.Value,
                proof.Value,
                license.Value,
                vehicle.Value));
    }

    private async Task<Result<KycDocumentSasDto?>> TrySasAsync(
        string? key,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(key))
            return Result<KycDocumentSasDto?>.Success(null);

        string trimmed = key.Trim();
        try
        {
            string url = await _fileStorage
                .GenerateSasUrlAsync(trimmed, expiresAt, cancellationToken)
                .ConfigureAwait(false);

            return Result<KycDocumentSasDto?>.Success(new KycDocumentSasDto(trimmed, url, expiresAt));
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            _logger.LogWarning(ex, "Failed to generate read SAS for KYC document key {Key}.", trimmed);
            return Result<KycDocumentSasDto?>.Failure(
                AdminKycErrorCodes.SasGenerationFailed,
                "Could not generate a temporary URL for one or more KYC documents.");
        }
    }
}
