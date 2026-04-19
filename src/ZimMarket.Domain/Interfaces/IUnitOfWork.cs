using ZimMarket.Domain.Entities.Users;
using ZimMarket.Domain.Interfaces.Repositories;

namespace ZimMarket.Domain.Interfaces;

public interface IUnitOfWork
{
    IUserRepository<Customer> Customers { get; }

    IUserRepository<Seller> Sellers { get; }

    IUserRepository<Driver> Drivers { get; }

    IProductRepository Products { get; }

    ICategoryRepository Categories { get; }

    IOrderRepository Orders { get; }

    IPaymentIdempotencyRepository PaymentIdempotency { get; }

    IDeliveryBatchRepository DeliveryBatches { get; }

    IWarehouseItemRepository WarehouseItems { get; }

    IPendingKycReadRepository PendingKyc { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    Task CommitAsync(CancellationToken cancellationToken = default);

    Task RollbackAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs <paramref name="operation"/> inside one database transaction, using the EF execution strategy when configured
    /// (required when Npgsql retry-on-failure is enabled).
    /// </summary>
    Task<T> RunInTransactionAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default);
}
