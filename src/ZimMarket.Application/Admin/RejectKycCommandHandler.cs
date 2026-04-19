using MediatR;
using Microsoft.Extensions.Logging;
using ZimMarket.Application.Common;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Application.Common.Models;
using ZimMarket.Domain.Entities.Users;
using ZimMarket.Domain.Enums;
using ZimMarket.Domain.Exceptions;
using ZimMarket.Domain.Interfaces;

namespace ZimMarket.Application.Admin;

public sealed class RejectKycCommandHandler : IRequestHandler<RejectKycCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<RejectKycCommandHandler> _logger;

    public RejectKycCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        ILogger<RejectKycCommandHandler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result> Handle(RejectKycCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated
            || _currentUser.UserId == Guid.Empty
            || (_currentUser.Role != UserRole.Admin && _currentUser.Role != UserRole.SuperAdmin))
        {
            _logger.LogDebug("Reject KYC rejected: caller is not an admin or super admin.");
            return Result.Failure(
                AdminKycErrorCodes.Forbidden,
                "Only administrators or super administrators can reject KYC submissions.");
        }

        string reason = request.Reason.Trim();

        return request.Role switch
        {
            UserRole.Seller => await RejectSellerAsync(request.UserId, reason, cancellationToken).ConfigureAwait(false),
            UserRole.Driver => await RejectDriverAsync(request.UserId, reason, cancellationToken).ConfigureAwait(false),
            _ => Result.Failure(
                AdminKycErrorCodes.CannotReject,
                "Only seller or driver KYC can be rejected with this command.")
        };
    }

    private async Task<Result> RejectSellerAsync(Guid sellerId, string reason, CancellationToken cancellationToken)
    {
        Seller? seller = await _unitOfWork.Sellers
            .GetByIdAsync(sellerId, cancellationToken)
            .ConfigureAwait(false);

        if (seller is null)
        {
            _logger.LogWarning("Reject seller KYC: no seller record for user {UserId}.", sellerId);
            return Result.Failure("Kyc.SellerNotFound", "Seller profile was not found.");
        }

        try
        {
            seller.Reject(reason);
        }
        catch (DomainException ex)
        {
            _logger.LogDebug(ex, "Reject seller KYC rejected by domain rules for seller {SellerId}.", sellerId);
            return Result.Failure(AdminKycErrorCodes.CannotReject, ex.Message);
        }

        await _unitOfWork.Sellers.UpdateAsync(seller, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Seller {SellerId} KYC rejected by admin {AdminId}.", sellerId, _currentUser.UserId);
        return Result.Success();
    }

    private async Task<Result> RejectDriverAsync(Guid driverId, string reason, CancellationToken cancellationToken)
    {
        Driver? driver = await _unitOfWork.Drivers
            .GetByIdAsync(driverId, cancellationToken)
            .ConfigureAwait(false);

        if (driver is null)
        {
            _logger.LogWarning("Reject driver KYC: no driver record for user {UserId}.", driverId);
            return Result.Failure("Kyc.DriverNotFound", "Driver profile was not found.");
        }

        try
        {
            driver.Reject(reason);
        }
        catch (DomainException ex)
        {
            _logger.LogDebug(ex, "Reject driver KYC rejected by domain rules for driver {DriverId}.", driverId);
            return Result.Failure(AdminKycErrorCodes.CannotReject, ex.Message);
        }

        await _unitOfWork.Drivers.UpdateAsync(driver, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Driver {DriverId} KYC rejected by admin {AdminId}.", driverId, _currentUser.UserId);
        return Result.Success();
    }
}
