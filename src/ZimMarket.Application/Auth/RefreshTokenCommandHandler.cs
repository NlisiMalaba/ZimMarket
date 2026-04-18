using System.Globalization;
using MediatR;
using Microsoft.Extensions.Logging;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Application.Common.Models;
using ZimMarket.Domain.Entities.Users;
using ZimMarket.Domain.Interfaces.Repositories;

namespace ZimMarket.Application.Auth;

public sealed class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<AuthTokensDto>>
{
    private readonly IUserLoginRepository _userLogin;
    private readonly IJwtService _jwtService;
    private readonly ILogger<RefreshTokenCommandHandler> _logger;

    public RefreshTokenCommandHandler(
        IUserLoginRepository userLogin,
        IJwtService jwtService,
        ILogger<RefreshTokenCommandHandler> logger)
    {
        _userLogin = userLogin ?? throw new ArgumentNullException(nameof(userLogin));
        _jwtService = jwtService ?? throw new ArgumentNullException(nameof(jwtService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<AuthTokensDto>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        AccessTokenForRefreshPrincipal? access = _jwtService.TryValidateAccessTokenForRefresh(request.AccessToken);
        if (access is null)
        {
            _logger.LogDebug("Refresh rejected: access token failed signature or issuer validation.");
            return Result<AuthTokensDto>.Failure(
                "Auth.InvalidAccessToken",
                "Access token is invalid or could not be verified.");
        }

        if (DateTimeOffset.UtcNow < access.AccessTokenExpiresAtUtc)
        {
            _logger.LogDebug("Refresh rejected: access token is not yet expired.");
            return Result<AuthTokensDto>.Failure(
                "Auth.AccessTokenNotExpired",
                "The access token is still valid; refresh is only allowed after it has expired.");
        }

        string? sub = access.Principal.FindFirst("sub")?.Value;

        if (string.IsNullOrWhiteSpace(sub) || !Guid.TryParse(sub, CultureInfo.InvariantCulture, out Guid userId))
        {
            _logger.LogDebug("Refresh rejected: access token missing subject.");
            return Result<AuthTokensDto>.Failure(
                "Auth.InvalidAccessToken",
                "Access token is invalid or could not be verified.");
        }

        User? user = await _userLogin.GetTrackedByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            _logger.LogDebug("Refresh rejected: user {UserId} not found.", userId);
            return Result<AuthTokensDto>.Failure(
                "Auth.InvalidRefreshToken",
                "Refresh token is invalid or has been revoked.");
        }

        if (!user.IsActive)
        {
            _logger.LogDebug("Refresh rejected: inactive user {UserId}.", userId);
            return Result<AuthTokensDto>.Failure("Auth.AccountDisabled", "This account has been disabled.");
        }

        if (string.IsNullOrWhiteSpace(user.RefreshTokenHash)
            || user.RefreshTokenExpiry is null
            || user.RefreshTokenExpiry <= DateTimeOffset.UtcNow)
        {
            _logger.LogDebug("Refresh rejected: no valid refresh session for user {UserId}.", userId);
            return Result<AuthTokensDto>.Failure(
                "Auth.InvalidRefreshToken",
                "Refresh token is invalid or has been revoked.");
        }

        if (!_jwtService.VerifyRefreshToken(request.RefreshToken, user.RefreshTokenHash))
        {
            _logger.LogDebug("Refresh rejected: refresh token mismatch for user {UserId}.", userId);
            return Result<AuthTokensDto>.Failure(
                "Auth.InvalidRefreshToken",
                "Refresh token is invalid or has been revoked.");
        }

        string newRefresh = _jwtService.GenerateRefreshToken();
        user.SetRefreshToken(
            _jwtService.HashRefreshTokenForStorage(newRefresh),
            _jwtService.GetRefreshTokenExpiresAtUtc());

        string newAccess = _jwtService.GenerateAccessToken(
            user.Id,
            user.Email,
            user.Role,
            user.KycStatus);

        return Result<AuthTokensDto>.Success(
            new AuthTokensDto
            {
                AccessToken = newAccess,
                RefreshToken = newRefresh,
                KycStatus = user.KycStatus
            });
    }
}
