using MediatR;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Application.Common.Models;
using ZimMarket.Domain.Entities.Users;
using ZimMarket.Domain.Enums;
using ZimMarket.Domain.Interfaces;

namespace ZimMarket.Application.Sellers;

public sealed class GetSellerVerificationQueryHandler
    : IRequestHandler<GetSellerVerificationQuery, Result<SellerVerificationDto>>
{
    private readonly ICurrentUser _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public GetSellerVerificationQueryHandler(ICurrentUser currentUser, IUnitOfWork unitOfWork)
    {
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<Result<SellerVerificationDto>> Handle(
        GetSellerVerificationQuery request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.Role != UserRole.Seller)
        {
            return Result<SellerVerificationDto>.Failure(
                "Seller.Forbidden",
                "Only authenticated sellers can view verification status.");
        }

        Seller? seller = await _unitOfWork.Sellers
            .GetByIdAsync(_currentUser.UserId, cancellationToken)
            .ConfigureAwait(false);

        if (seller is null)
        {
            return Result<SellerVerificationDto>.Failure(
                "Seller.NotFound",
                "Seller profile was not found.");
        }

        return Result<SellerVerificationDto>.Success(
            new SellerVerificationDto
            {
                KycStatus = seller.KycStatus,
                RejectionReason = seller.RejectionReason
            });
    }
}
