using MediatR;
using Microsoft.AspNetCore.Identity;
using ZimMarket.Application.Common;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Application.Common.Models;
using ZimMarket.Domain.Entities.Users;
using ZimMarket.Domain.Enums;
using ZimMarket.Domain.Interfaces;
using ZimMarket.Domain.Interfaces.Repositories;

namespace ZimMarket.Application.Auth;

public sealed class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserLoginRepository _userLoginRepository;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly IAuthTokenService _authTokenService;

    public ResetPasswordCommandHandler(
        IUnitOfWork unitOfWork,
        IUserLoginRepository userLoginRepository,
        IPasswordHasher<User> passwordHasher,
        IAuthTokenService authTokenService)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _userLoginRepository = userLoginRepository ?? throw new ArgumentNullException(nameof(userLoginRepository));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        _authTokenService = authTokenService ?? throw new ArgumentNullException(nameof(authTokenService));
    }

    public async Task<Result> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        string tokenHash = _authTokenService.HashToken(request.Token);
        AuthToken? token = await _unitOfWork.AuthTokens
            .GetActiveByHashAsync(tokenHash, AuthTokenPurpose.PasswordReset, cancellationToken)
            .ConfigureAwait(false);

        if (token is null)
            return Result.Failure(AuthErrorCodes.AuthPasswordResetInvalid, "Password reset token is invalid or expired.");

        User? user = await _userLoginRepository.GetTrackedByIdAsync(token.UserId, cancellationToken).ConfigureAwait(false);
        if (user is null)
            return Result.Failure(AuthErrorCodes.AuthPasswordResetInvalid, "Password reset token is invalid or expired.");

        DateTimeOffset now = DateTimeOffset.UtcNow;
        string newHash = _passwordHasher.HashPassword(user, request.NewPassword);
        user.SetPasswordHash(newHash);
        user.ClearRefreshToken();
        token.MarkConsumed(now);

        await _unitOfWork.AuthTokens
            .RevokeActiveTokensAsync(user.Id, AuthTokenPurpose.PasswordReset, now, cancellationToken)
            .ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
