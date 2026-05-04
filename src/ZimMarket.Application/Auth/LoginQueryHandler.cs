using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using ZimMarket.Application.Common;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Application.Common.Models;
using ZimMarket.Domain.Entities.Users;
using ZimMarket.Domain.Enums;
using ZimMarket.Domain.Interfaces;
using ZimMarket.Domain.Interfaces.Repositories;

namespace ZimMarket.Application.Auth;

public sealed class LoginQueryHandler : IRequestHandler<LoginQuery, Result<AuthTokensDto>>
{
    private readonly IUserLoginRepository _userLogin;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly IJwtService _jwtService;
    private readonly ILogger<LoginQueryHandler> _logger;

    public LoginQueryHandler(
        IUserLoginRepository userLogin,
        IUnitOfWork unitOfWork,
        IPasswordHasher<User> passwordHasher,
        IJwtService jwtService,
        ILogger<LoginQueryHandler> logger)
    {
        _userLogin = userLogin ?? throw new ArgumentNullException(nameof(userLogin));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        _jwtService = jwtService ?? throw new ArgumentNullException(nameof(jwtService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<AuthTokensDto>> Handle(LoginQuery request, CancellationToken cancellationToken)
    {
        string normalizedEmail = request.Email.Trim().ToLowerInvariant();

        if (!string.IsNullOrWhiteSpace(request.DeviceInfo))
        {
            _logger.LogInformation(
                "Login attempt for {Email} with device info length {Length}.",
                normalizedEmail,
                request.DeviceInfo.Trim().Length);
        }

        User? user = await _userLogin
            .GetTrackedByNormalizedEmailAsync(normalizedEmail, cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
        {
            _logger.LogDebug("Login failed: unknown email {Email}.", normalizedEmail);
            return Result<AuthTokensDto>.Failure(AuthErrorCodes.AuthInvalidCredentials, "Invalid email or password.");
        }

        PasswordVerificationResult verification = _passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            request.Password);

        if (verification is PasswordVerificationResult.Failed)
        {
            _logger.LogDebug("Login failed: bad password for {Email}.", normalizedEmail);
            return Result<AuthTokensDto>.Failure(AuthErrorCodes.AuthInvalidCredentials, "Invalid email or password.");
        }

        if (user.Role == UserRole.Admin)
        {
            AdminApprovalState? approvalState = await _unitOfWork.AdminApprovalStates
                .GetByUserIdAsync(user.Id, cancellationToken)
                .ConfigureAwait(false);

            if (approvalState is null || !approvalState.IsEmailVerified)
            {
                return Result<AuthTokensDto>.Failure(
                    AuthErrorCodes.AuthEmailVerificationRequired,
                    "Verify your email before signing in.");
            }

            if (!approvalState.IsApproved)
            {
                return Result<AuthTokensDto>.Failure(
                    AuthErrorCodes.AuthAdminApprovalPending,
                    "Your account is awaiting super admin approval.");
            }

            if (!user.IsActive)
            {
                _logger.LogDebug("Login failed: inactive admin account {Email}.", normalizedEmail);
                return Result<AuthTokensDto>.Failure(AuthErrorCodes.AuthAccountLocked, "This account has been disabled.");
            }
        }
        else if (!user.IsActive)
        {
            _logger.LogDebug("Login failed: inactive account {Email}.", normalizedEmail);
            return Result<AuthTokensDto>.Failure(AuthErrorCodes.AuthAccountLocked, "This account has been disabled.");
        }

        string refreshToken = _jwtService.GenerateRefreshToken();
        user.SetRefreshToken(
            _jwtService.HashRefreshTokenForStorage(refreshToken),
            _jwtService.GetRefreshTokenExpiresAtUtc());

        string accessToken = _jwtService.GenerateAccessToken(
            user.Id,
            user.Email,
            user.Role,
            user.KycStatus);

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<AuthTokensDto>.Success(
            new AuthTokensDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                KycStatus = user.KycStatus
            });
    }
}
