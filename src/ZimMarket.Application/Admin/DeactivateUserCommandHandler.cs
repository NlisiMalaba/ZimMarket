using MediatR;
using Microsoft.Extensions.Logging;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Application.Common.Models;
using ZimMarket.Domain.Entities.Users;
using ZimMarket.Domain.Enums;
using ZimMarket.Domain.Interfaces.Repositories;

namespace ZimMarket.Application.Admin;

public sealed class DeactivateUserCommandHandler : IRequestHandler<DeactivateUserCommand, Result>
{
    private readonly ICurrentUser _currentUser;
    private readonly IUserLoginRepository _userLogin;
    private readonly ILogger<DeactivateUserCommandHandler> _logger;

    public DeactivateUserCommandHandler(
        ICurrentUser currentUser,
        IUserLoginRepository userLogin,
        ILogger<DeactivateUserCommandHandler> logger)
    {
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _userLogin = userLogin ?? throw new ArgumentNullException(nameof(userLogin));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result> Handle(DeactivateUserCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated
            || _currentUser.UserId == Guid.Empty
            || (_currentUser.Role != UserRole.Admin && _currentUser.Role != UserRole.SuperAdmin))
        {
            _logger.LogDebug("Deactivate user rejected: caller is not an administrator.");
            return Result.Failure(
                UserLifecycleErrorCodes.Forbidden,
                "Only administrators can deactivate user accounts.");
        }

        if (request.UserId == _currentUser.UserId)
        {
            _logger.LogDebug("Deactivate user rejected: caller attempted to deactivate their own account.");
            return Result.Failure(
                UserLifecycleErrorCodes.CannotActOnSelf,
                "You cannot deactivate your own account.");
        }

        User? user = await _userLogin.GetTrackedByIdAsync(request.UserId, cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            _logger.LogWarning("Deactivate user: no user record for id {UserId}.", request.UserId);
            return Result.Failure(UserLifecycleErrorCodes.UserNotFound, "User was not found.");
        }

        if (!UserLifecycleAuthorization.CallerMayManageTarget(_currentUser.Role, user.Role))
        {
            _logger.LogDebug(
                "Deactivate user rejected: caller role {CallerRole} cannot manage target role {TargetRole}.",
                _currentUser.Role,
                user.Role);

            return Result.Failure(
                UserLifecycleErrorCodes.InsufficientPrivilegeForTarget,
                "You cannot deactivate this account type.");
        }

        user.ClearRefreshToken();
        user.Deactivate();

        _logger.LogInformation(
            "User {UserId} ({Email}) deactivated by {ActorId} ({ActorRole}).",
            user.Id,
            user.Email,
            _currentUser.UserId,
            _currentUser.Role);

        return Result.Success();
    }
}
