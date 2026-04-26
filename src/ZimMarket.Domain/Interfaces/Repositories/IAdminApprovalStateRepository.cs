using ZimMarket.Domain.Entities.Users;

namespace ZimMarket.Domain.Interfaces.Repositories;

public interface IAdminApprovalStateRepository
{
    Task AddAsync(AdminApprovalState state, CancellationToken cancellationToken = default);

    Task<AdminApprovalState?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<bool> ExistsAnySuperAdminAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetSuperAdminEmailsAsync(CancellationToken cancellationToken = default);
}
