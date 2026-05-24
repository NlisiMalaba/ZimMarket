using MediatR;
using Microsoft.AspNetCore.Identity;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Application.Common.Models;
using ZimMarket.Domain.Entities.Users;
using ZimMarket.Domain.Enums;
using ZimMarket.Domain.Interfaces;
using ZimMarket.Domain.Interfaces.Repositories;

namespace ZimMarket.Application.Sellers;

public sealed class ChangeSellerPasswordCommandHandler : IRequestHandler<ChangeSellerPasswordCommand, Result>
{
    private readonly ICurrentUser _currentUser;
    private readonly IUserLoginRepository _userLoginRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher<User> _passwordHasher;

    public ChangeSellerPasswordCommandHandler(
        ICurrentUser currentUser,
        IUserLoginRepository userLoginRepository,
        IUnitOfWork unitOfWork,
        IPasswordHasher<User> passwordHasher)
    {
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _userLoginRepository = userLoginRepository ?? throw new ArgumentNullException(nameof(userLoginRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
    }

    public async Task<Result> Handle(ChangeSellerPasswordCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.Role != UserRole.Seller)
        {
            return Result.Failure("Seller.Forbidden", "Only authenticated sellers can change their password.");
        }

        User? user = await _userLoginRepository
            .GetTrackedByIdAsync(_currentUser.UserId, cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
        {
            return Result.Failure("Seller.NotFound", "Seller profile was not found.");
        }

        PasswordVerificationResult verification = _passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            request.CurrentPassword);

        if (verification is PasswordVerificationResult.Failed)
        {
            return Result.Failure("Seller.InvalidPassword", "Current password is incorrect.");
        }

        string newHash = _passwordHasher.HashPassword(user, request.NewPassword);
        user.SetPasswordHash(newHash);
        user.ClearRefreshToken();

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
