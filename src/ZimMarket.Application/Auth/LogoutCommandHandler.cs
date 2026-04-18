using MediatR;
using ZimMarket.Application.Common.Models;
using ZimMarket.Domain.Entities.Users;
using ZimMarket.Domain.Interfaces.Repositories;

namespace ZimMarket.Application.Auth;

public sealed class LogoutCommandHandler : IRequestHandler<LogoutCommand, Result>
{
    private readonly IUserLoginRepository _userLogin;

    public LogoutCommandHandler(IUserLoginRepository userLogin)
    {
        _userLogin = userLogin ?? throw new ArgumentNullException(nameof(userLogin));
    }

    public async Task<Result> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        User? user = await _userLogin
            .GetTrackedUserByRefreshTokenAsync(request.RefreshToken, cancellationToken)
            .ConfigureAwait(false);

        if (user is not null)
            user.ClearRefreshToken();

        return Result.Success();
    }
}
