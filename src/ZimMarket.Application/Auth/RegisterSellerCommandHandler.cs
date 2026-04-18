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
using ZimMarket.Domain.ValueObjects;

namespace ZimMarket.Application.Auth;

public sealed class RegisterSellerCommandHandler : IRequestHandler<RegisterSellerCommand, Result<AuthTokensDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserIdentityReadRepository _userIdentityRead;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly IJwtService _jwtService;
    private readonly ILogger<RegisterSellerCommandHandler> _logger;

    public RegisterSellerCommandHandler(
        IUnitOfWork unitOfWork,
        IUserIdentityReadRepository userIdentityRead,
        IPasswordHasher<User> passwordHasher,
        IJwtService jwtService,
        ILogger<RegisterSellerCommandHandler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _userIdentityRead = userIdentityRead ?? throw new ArgumentNullException(nameof(userIdentityRead));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        _jwtService = jwtService ?? throw new ArgumentNullException(nameof(jwtService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<AuthTokensDto>> Handle(
        RegisterSellerCommand request,
        CancellationToken cancellationToken)
    {
        string normalizedEmail = request.Email.Trim().ToLowerInvariant();
        string fullName = request.FullName.Trim();
        string businessName = request.BusinessName.Trim();

        var phoneResult = PhoneNumber.Create(request.Phone.Trim());
        if (phoneResult.IsFailure)
        {
            _logger.LogDebug("Seller registration rejected: invalid phone format.");
            return Result<AuthTokensDto>.ValidationFailure(
            [
                new ValidationError(
                    nameof(RegisterSellerCommand.Phone),
                    string.Join("; ", phoneResult.Errors))
            ]);
        }

        PhoneNumber phone = phoneResult.Value!;

        if (await _userIdentityRead.ExistsWithEmailAsync(normalizedEmail, cancellationToken).ConfigureAwait(false))
        {
            _logger.LogDebug("Seller registration rejected: email {Email} already exists.", normalizedEmail);
            return Result<AuthTokensDto>.Failure(AuthErrorCodes.UserAlreadyExists, "This email is already registered.");
        }

        if (await _userIdentityRead.ExistsWithPhoneAsync(phone, cancellationToken).ConfigureAwait(false))
        {
            _logger.LogDebug("Seller registration rejected: phone already exists.");
            return Result<AuthTokensDto>.Failure(AuthErrorCodes.UserPhoneAlreadyExists, "This phone number is already registered.");
        }

        DateTimeOffset now = DateTimeOffset.UtcNow;
        Guid id = Guid.NewGuid();

        var hashSource = new Seller(
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
            businessName,
            nationalIdDocumentKey: string.Empty,
            proofOfResidenceDocumentKey: string.Empty,
            isApproved: false,
            rejectionReason: null);

        string passwordHash = _passwordHasher.HashPassword(hashSource, request.Password);

        Seller seller = Seller.CreateNewRegistration(
            id,
            normalizedEmail,
            fullName,
            phone,
            passwordHash,
            now,
            now,
            businessName);

        string refreshToken = _jwtService.GenerateRefreshToken();
        string refreshHash = _jwtService.HashRefreshTokenForStorage(refreshToken);
        seller.SetRefreshToken(refreshHash, _jwtService.GetRefreshTokenExpiresAtUtc());

        string accessToken = _jwtService.GenerateAccessToken(
            seller.Id,
            seller.Email,
            UserRole.Seller,
            seller.KycStatus);

        await _unitOfWork.Sellers.AddAsync(seller, cancellationToken).ConfigureAwait(false);

        return Result<AuthTokensDto>.Success(
            new AuthTokensDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                KycStatus = seller.KycStatus
            });
    }
}
