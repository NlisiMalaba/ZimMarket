using Microsoft.EntityFrameworkCore;
using ZimMarket.Domain.Entities.Users;
using ZimMarket.Domain.Enums;
using ZimMarket.Domain.Interfaces.Repositories;

namespace ZimMarket.Infrastructure.Persistence.Repositories;

internal sealed class AdminApprovalStateRepository : IAdminApprovalStateRepository
{
    private readonly AppDbContext _dbContext;

    public AdminApprovalStateRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public Task AddAsync(AdminApprovalState state, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);
        return _dbContext.Set<AdminApprovalState>().AddAsync(state, cancellationToken).AsTask();
    }

    public Task<AdminApprovalState?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Set<AdminApprovalState>()
            .FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);
    }

    public Task<bool> ExistsAnySuperAdminAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.Set<User>()
            .AnyAsync(u => u.Role == UserRole.SuperAdmin, cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetSuperAdminEmailsAsync(CancellationToken cancellationToken = default)
    {
        List<string> emails = await _dbContext.Set<User>()
            .Where(u => u.Role == UserRole.SuperAdmin && u.IsActive)
            .Select(u => u.Email)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return emails;
    }
}
