using MediatR;
using Microsoft.Extensions.Logging;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Application.Common.Models;
using ZimMarket.Domain.Entities.Users;
using ZimMarket.Domain.Enums;
using ZimMarket.Domain.Exceptions;
using ZimMarket.Domain.Interfaces;

namespace ZimMarket.Application.Auth;

public sealed class SubmitDriverKycCommandHandler : IRequestHandler<SubmitDriverKycCommand, Result>
{
    private readonly ICurrentUser _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileStorage _fileStorage;
    private readonly ILogger<SubmitDriverKycCommandHandler> _logger;

    public SubmitDriverKycCommandHandler(
        ICurrentUser currentUser,
        IUnitOfWork unitOfWork,
        IFileStorage fileStorage,
        ILogger<SubmitDriverKycCommandHandler> logger)
    {
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _fileStorage = fileStorage ?? throw new ArgumentNullException(nameof(fileStorage));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result> Handle(SubmitDriverKycCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.Role != UserRole.Driver)
        {
            _logger.LogDebug("Driver KYC submit rejected: caller is not an authenticated driver.");
            return Result.Failure("Kyc.Forbidden", "Only authenticated drivers can submit KYC documents.");
        }

        string licenseDocKey = request.LicenseDocKey.Trim();
        string vehicleDocKey = request.VehicleDocKey.Trim();
        string licenseNumber = request.LicenseNumber.Trim();
        string vehicleRegistration = request.VehicleRegistration.Trim();

        Driver? driver = await _unitOfWork.Drivers
            .GetByIdAsync(_currentUser.UserId, cancellationToken)
            .ConfigureAwait(false);

        if (driver is null)
        {
            _logger.LogWarning("Driver KYC submit failed: no driver record for user {UserId}.", _currentUser.UserId);
            return Result.Failure("Kyc.DriverNotFound", "Driver profile was not found.");
        }

        Result? licenseDocCheck = await CheckBlobExistsAsync(
                licenseDocKey,
                nameof(SubmitDriverKycCommand.LicenseDocKey),
                "The driver license document was not found in storage. Upload the file first.",
                cancellationToken)
            .ConfigureAwait(false);

        if (licenseDocCheck is not null)
        {
            _logger.LogDebug("Driver KYC submit rejected: license document key invalid or missing for driver {DriverId}.", driver.Id);
            return licenseDocCheck;
        }

        Result? vehicleDocCheck = await CheckBlobExistsAsync(
                vehicleDocKey,
                nameof(SubmitDriverKycCommand.VehicleDocKey),
                "The vehicle document was not found in storage. Upload the file first.",
                cancellationToken)
            .ConfigureAwait(false);

        if (vehicleDocCheck is not null)
        {
            _logger.LogDebug("Driver KYC submit rejected: vehicle document key invalid or missing for driver {DriverId}.", driver.Id);
            return vehicleDocCheck;
        }

        try
        {
            driver.SubmitKyc(licenseNumber, licenseDocKey, vehicleRegistration, vehicleDocKey);
        }
        catch (DomainException ex)
        {
            _logger.LogDebug(ex, "Driver KYC submit rejected by domain rules for driver {DriverId}.", driver.Id);
            return Result.Failure("Kyc.AlreadySubmitted", ex.Message);
        }

        await _unitOfWork.Drivers.UpdateAsync(driver, cancellationToken).ConfigureAwait(false);

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
