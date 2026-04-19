using Microsoft.EntityFrameworkCore;
using ZimMarket.Domain.Entities.Users;
using ZimMarket.Domain.Enums;
using ZimMarket.Domain.Interfaces.Repositories;
using ZimMarket.Domain.ReadModels;
using ZimMarket.Shared;

namespace ZimMarket.Infrastructure.Persistence.Repositories;

internal sealed class PendingKycReadRepository : IPendingKycReadRepository
{
    private readonly AppDbContext _dbContext;

    public PendingKycReadRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    /// <inheritdoc />
    public Task<PagedList<PendingKycQueueRow>> GetPagedPendingReviewAsync(
        UserRole role,
        PaginationParams pagination,
        CancellationToken cancellationToken = default) =>
        role switch
        {
            UserRole.Seller => GetSellersPendingAsync(pagination, cancellationToken),
            UserRole.Driver => GetDriversPendingAsync(pagination, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(
                nameof(role),
                "Only Seller or Driver roles are supported for the pending KYC queue.")
        };

    private async Task<PagedList<PendingKycQueueRow>> GetSellersPendingAsync(
        PaginationParams pagination,
        CancellationToken cancellationToken)
    {
        IQueryable<Seller> query = _dbContext.Set<Seller>()
            .AsNoTracking()
            .Where(s => s.KycStatus == KycStatus.PendingReview)
            .OrderByDescending(s => s.UpdatedAt);

        long totalCount = await query.LongCountAsync(cancellationToken).ConfigureAwait(false);

        List<PendingKycQueueRow> items = await query
            .Skip((pagination.Page - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .Select(s => new PendingKycQueueRow(
                s.Id,
                s.Email,
                s.FullName,
                UserRole.Seller,
                s.BusinessName,
                null,
                null,
                s.NationalIdDocumentKey,
                s.ProofOfResidenceDocumentKey,
                null,
                null))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedList<PendingKycQueueRow>(items, pagination.Page, pagination.PageSize, totalCount);
    }

    private async Task<PagedList<PendingKycQueueRow>> GetDriversPendingAsync(
        PaginationParams pagination,
        CancellationToken cancellationToken)
    {
        IQueryable<Driver> query = _dbContext.Set<Driver>()
            .AsNoTracking()
            .Where(d => d.KycStatus == KycStatus.PendingReview)
            .OrderByDescending(d => d.UpdatedAt);

        long totalCount = await query.LongCountAsync(cancellationToken).ConfigureAwait(false);

        List<PendingKycQueueRow> items = await query
            .Skip((pagination.Page - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .Select(d => new PendingKycQueueRow(
                d.Id,
                d.Email,
                d.FullName,
                UserRole.Driver,
                null,
                d.LicenseNumber,
                d.VehicleRegistration,
                null,
                null,
                d.LicenseDocumentKey,
                d.VehicleDocumentKey))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedList<PendingKycQueueRow>(items, pagination.Page, pagination.PageSize, totalCount);
    }
}
