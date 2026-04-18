using MediatR;
using Microsoft.Extensions.Logging;
using ZimMarket.Application.Auth;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Application.Common.Models;
using ZimMarket.Domain.Entities.Catalogue;
using ZimMarket.Domain.Enums;
using ZimMarket.Domain.Interfaces;

namespace ZimMarket.Application.Catalogue;

public sealed class DeleteProductCommandHandler : IRequestHandler<DeleteProductCommand, Result>
{
    private readonly ICurrentUser _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;
    private readonly ILogger<DeleteProductCommandHandler> _logger;

    public DeleteProductCommandHandler(
        ICurrentUser currentUser,
        IUnitOfWork unitOfWork,
        ICacheService cacheService,
        ILogger<DeleteProductCommandHandler> logger)
    {
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        if (!IsSellerKycApproved())
        {
            _logger.LogDebug("Delete product rejected: caller is not a seller with approved KYC.");
            return Result.Failure("Products.Forbidden", "Only KYC-approved sellers can delete products.");
        }

        Product? product = await _unitOfWork.Products.GetByIdAsync(request.ProductId, cancellationToken).ConfigureAwait(false);
        if (product is null)
            return Result.Failure("Products.NotFound", "Product was not found.");

        if (product.SellerId != _currentUser.UserId)
            return Result.Failure("Products.Forbidden", "You can only delete your own products.");

        product.Delete();
        await _unitOfWork.Products.UpdateAsync(product, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await _cacheService.RemoveAsync(GetProductCacheKey(product.Id), cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }

    private bool IsSellerKycApproved()
    {
        if (!_currentUser.IsAuthenticated || _currentUser.Role != UserRole.Seller || _currentUser.UserId == Guid.Empty)
            return false;

        string? kycStatus = _currentUser.GetClaim(AuthClaimTypes.KycStatus);
        return string.Equals(kycStatus, KycStatus.Approved.ToString(), StringComparison.Ordinal);
    }

    private static string GetProductCacheKey(Guid productId) => $"product:{productId:D}";
}
