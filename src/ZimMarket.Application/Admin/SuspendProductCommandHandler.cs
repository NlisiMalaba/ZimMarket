using MediatR;
using Microsoft.Extensions.Logging;
using ZimMarket.Application.Common;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Application.Common.Models;
using ZimMarket.Domain.Entities.Catalogue;
using ZimMarket.Domain.Enums;
using ZimMarket.Domain.Exceptions;
using ZimMarket.Domain.Interfaces;

namespace ZimMarket.Application.Admin;

public sealed class SuspendProductCommandHandler : IRequestHandler<SuspendProductCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly ICacheService _cacheService;
    private readonly ILogger<SuspendProductCommandHandler> _logger;

    public SuspendProductCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        ICacheService cacheService,
        ILogger<SuspendProductCommandHandler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result> Handle(SuspendProductCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated
            || _currentUser.UserId == Guid.Empty
            || (_currentUser.Role != UserRole.Admin && _currentUser.Role != UserRole.SuperAdmin))
        {
            _logger.LogDebug("Suspend product rejected: caller is not an admin or super admin.");
            return Result.Failure(
                AdminProductErrorCodes.Forbidden,
                "Only administrators or super administrators can suspend product listings.");
        }

        Product? product = await _unitOfWork.Products
            .GetByIdAsync(request.ProductId, cancellationToken)
            .ConfigureAwait(false);

        if (product is null)
        {
            _logger.LogWarning("Suspend product: product {ProductId} was not found.", request.ProductId);
            return Result.Failure("Products.NotFound", "Product was not found.");
        }

        try
        {
            product.Suspend(request.Reason.Trim());
        }
        catch (DomainException ex)
        {
            _logger.LogDebug(ex, "Suspend product rejected by domain rules for product {ProductId}.", request.ProductId);
            return Result.Failure(AdminProductErrorCodes.CannotSuspend, ex.Message);
        }

        await _unitOfWork.Products.UpdateAsync(product, cancellationToken).ConfigureAwait(false);

        await _cacheService
            .RemoveAsync(GetProductCacheKey(product.Id), cancellationToken)
            .ConfigureAwait(false);

        _logger.LogInformation(
            "Product {ProductId} suspended by admin {AdminId}.",
            request.ProductId,
            _currentUser.UserId);

        return Result.Success();
    }

    private static string GetProductCacheKey(Guid productId) => $"product:{productId:D}";
}
