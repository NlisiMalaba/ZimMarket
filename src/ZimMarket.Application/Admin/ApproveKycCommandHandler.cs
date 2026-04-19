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

public sealed class ApproveKycCommandHandler : IRequestHandler<ApproveKycCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<ApproveKycCommandHandler> _logger;

    public ApproveKycCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        ILogger<ApproveKycCommandHandler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result> Handle(ApproveKycCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated
            || _currentUser.UserId == Guid.Empty
            || _currentUser.Role != UserRole.Admin)
        {
            _logger.LogDebug("Approve KYC rejected: caller is not an admin.");
            return Result.Failure(
                AdminKycErrorCodes.Forbidden,
                "Only administrators can approve KYC submissions.");
        }

        return request.Role switch
        {
            UserRole.Seller => await ApproveSellerAsync(request.UserId, cancellationToken).ConfigureAwait(false),
            UserRole.Driver => await ApproveDriverAsync(request.UserId, cancellationToken).ConfigureAwait(false),
            _ => Result.Failure(
                AdminKycErrorCodes.CannotApprove,
                "Only seller or driver KYC can be approved with this command.")
        };
    }

    private async Task<Result> ApproveSellerAsync(Guid sellerId, CancellationToken cancellationToken)
    {
        Seller? seller = await _unitOfWork.Sellers
            .GetByIdAsync(sellerId, cancellationToken)
            .ConfigureAwait(false);

        if (seller is null)
        {
            _logger.LogWarning("Approve seller KYC: no seller record for user {UserId}.", sellerId);
            return Result.Failure("Kyc.SellerNotFound", "Seller profile was not found.");
        }

        try
        {
            seller.Approve();
        }
        catch (DomainException ex)
        {
            _logger.LogDebug(ex, "Approve seller KYC rejected by domain rules for seller {SellerId}.", sellerId);
            return Result.Failure(AdminKycErrorCodes.CannotApprove, ex.Message);
        }

        await _unitOfWork.Sellers.UpdateAsync(seller, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Seller {SellerId} KYC approved by admin {AdminId}.", sellerId, _currentUser.UserId);
        return Result.Success();
    }

    private async Task<Result> ApproveDriverAsync(Guid driverId, CancellationToken cancellationToken)
    {
        Driver? driver = await _unitOfWork.Drivers
            .GetByIdAsync(driverId, cancellationToken)
            .ConfigureAwait(false);

        if (driver is null)
        {
            _logger.LogWarning("Approve driver KYC: no driver record for user {UserId}.", driverId);
            return Result.Failure("Kyc.DriverNotFound", "Driver profile was not found.");
        }

        try
        {
            driver.Approve();
        }
        catch (DomainException ex)
        {
            _logger.LogDebug(ex, "Approve driver KYC rejected by domain rules for driver {DriverId}.", driverId);
            return Result.Failure(AdminKycErrorCodes.CannotApprove, ex.Message);
        }

        await _unitOfWork.Drivers.UpdateAsync(driver, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Driver {DriverId} KYC approved by admin {AdminId}.", driverId, _currentUser.UserId);
        return Result.Success();
    }
}
