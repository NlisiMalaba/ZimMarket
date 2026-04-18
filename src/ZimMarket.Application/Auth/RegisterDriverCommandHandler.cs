using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Application.Common.Models;
using ZimMarket.Domain.Entities.Users;
using ZimMarket.Domain.Enums;
using ZimMarket.Domain.Interfaces;
using ZimMarket.Domain.Interfaces.Repositories;
using ZimMarket.Domain.ValueObjects;

namespace ZimMarket.Application.Auth;

public sealed class RegisterDriverCommandHandler : IRequestHandler<RegisterDriverCommand, Result<AuthTokensDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserIdentityReadRepository _userIdentityRead;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly IJwtService _jwtService;
    private readonly ILogger<RegisterDriverCommandHandler> _logger;

    public RegisterDriverCommandHandler(
        IUnitOfWork unitOfWork,
        IUserIdentityReadRepository userIdentityRead,
        IPasswordHasher<User> passwordHasher,
        IJwtService jwtService,
        ILogger<RegisterDriverCommandHandler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _userIdentityRead = userIdentityRead ?? throw new ArgumentNullException(nameof(userIdentityRead));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        _jwtService = jwtService ?? throw new ArgumentNullException(nameof(jwtService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<AuthTokensDto>> Handle(
        RegisterDriverCommand request,
        CancellationToken cancellationToken)
    {
        string normalizedEmail = request.Email.Trim().ToLowerInvariant();
        string fullName = request.FullName.Trim();

        var phoneResult = PhoneNumber.Create(request.Phone.Trim());
        if (phoneResult.IsFailure)
        {
            _logger.LogDebug("Driver registration rejected: invalid phone format.");
            return Result<AuthTokensDto>.ValidationFailure(
            [
                new ValidationError(
                    nameof(RegisterDriverCommand.Phone),
                    string.Join("; ", phoneResult.Errors))
            ]);
        }

        PhoneNumber phone = phoneResult.Value!;

        if (await _userIdentityRead.ExistsWithEmailAsync(normalizedEmail, cancellationToken).ConfigureAwait(false))
        {
            _logger.LogDebug("Driver registration rejected: email {Email} already exists.", normalizedEmail);
            return Result<AuthTokensDto>.Failure("Auth.EmailTaken", "This email is already registered.");
        }

        if (await _userIdentityRead.ExistsWithPhoneAsync(phone, cancellationToken).ConfigureAwait(false))
        {
            _logger.LogDebug("Driver registration rejected: phone already exists.");
            return Result<AuthTokensDto>.Failure("Auth.PhoneTaken", "This phone number is already registered.");
        }

        DateTimeOffset now = DateTimeOffset.UtcNow;
        Guid id = Guid.NewGuid();
        string pendingMarker = id.ToString("N");

        var hashSource = new Driver(
            id,
            normalizedEmail,
            fullName,
            phone,
            passwordHash: "TEMP",
            KycStatus.NotSubmitted,
            isActive: true,
            refreshTokenHash: null,
            refreshTokenExpiry: null,
            createdAt: now,
            updatedAt: now,
            licenseNumber: $"pending-lic-{pendingMarker}",
            licenseDocumentKey: string.Empty,
            vehicleRegistration: $"pending-veh-{pendingMarker}",
            vehicleDocumentKey: string.Empty,
            DriverStatus.Offline,
            lastKnownLocation: null,
            isApproved: false,
            rejectionReason: null);

        string passwordHash = _passwordHasher.HashPassword(hashSource, request.Password);

        Driver driver = Driver.CreateNewRegistration(
            id,
            normalizedEmail,
            fullName,
            phone,
            passwordHash,
            now,
            now);

        string refreshToken = _jwtService.GenerateRefreshToken();
        string refreshHash = _jwtService.HashRefreshTokenForStorage(refreshToken);
        driver.SetRefreshToken(refreshHash, _jwtService.GetRefreshTokenExpiresAtUtc());

        string accessToken = _jwtService.GenerateAccessToken(
            driver.Id,
            driver.Email,
            UserRole.Driver,
            driver.KycStatus);

        await _unitOfWork.Drivers.AddAsync(driver, cancellationToken).ConfigureAwait(false);

        return Result<AuthTokensDto>.Success(
            new AuthTokensDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                KycStatus = driver.KycStatus
            });
    }
}
