using ZimMarket.Domain.Interfaces;
using ZimMarket.Domain.Interfaces.Repositories;

namespace ZimMarket.Infrastructure.Persistence;

/// <summary>
/// Fallback unit of work used when database infrastructure is not configured.
/// Accessing any repository or transaction method throws a descriptive exception.
/// </summary>
public sealed class UnavailableUnitOfWork : IUnitOfWork
{
    private static InvalidOperationException CreateUnavailableException() =>
        new("Database services are not configured. Set ConnectionStrings:DefaultConnection to enable persistence-backed operations.");

    public IUserRepository<Domain.Entities.Users.Customer> Customers => throw CreateUnavailableException();
    public IUserRepository<Domain.Entities.Users.Seller> Sellers => throw CreateUnavailableException();
    public IUserRepository<Domain.Entities.Users.Driver> Drivers => throw CreateUnavailableException();
    public IUserRepository<Domain.Entities.Users.AdminUser> Admins => throw CreateUnavailableException();
    public IUserRepository<Domain.Entities.Users.SuperAdminUser> SuperAdmins => throw CreateUnavailableException();
    public IDriverReadRepository DriverRead => throw CreateUnavailableException();
    public IProductRepository Products => throw CreateUnavailableException();
    public ICategoryRepository Categories => throw CreateUnavailableException();
    public IOrderRepository Orders => throw CreateUnavailableException();
    public IPaymentIdempotencyRepository PaymentIdempotency => throw CreateUnavailableException();
    public IDeliveryBatchRepository DeliveryBatches => throw CreateUnavailableException();
    public IWarehouseItemRepository WarehouseItems => throw CreateUnavailableException();
    public IPendingKycReadRepository PendingKyc => throw CreateUnavailableException();
    public IDashboardStatsReadRepository DashboardStats => throw CreateUnavailableException();
    public IDriverLocationRepository DriverLocations => throw CreateUnavailableException();
    public IAuthTokenRepository AuthTokens => throw CreateUnavailableException();
    public IAdminApprovalStateRepository AdminApprovalStates => throw CreateUnavailableException();

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        throw CreateUnavailableException();

    public Task BeginTransactionAsync(CancellationToken cancellationToken = default) =>
        throw CreateUnavailableException();

    public Task CommitAsync(CancellationToken cancellationToken = default) =>
        throw CreateUnavailableException();

    public Task RollbackAsync(CancellationToken cancellationToken = default) =>
        throw CreateUnavailableException();

    public Task<T> RunInTransactionAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default) =>
        throw CreateUnavailableException();
}
