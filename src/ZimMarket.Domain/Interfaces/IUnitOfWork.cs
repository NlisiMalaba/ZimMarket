using ZimMarket.Domain.Entities.Users;
using ZimMarket.Domain.Interfaces.Repositories;

namespace ZimMarket.Domain.Interfaces;

public interface IUnitOfWork
{
    IUserRepository<Customer> Customers { get; }

    IUserRepository<Seller> Sellers { get; }

    IUserRepository<Driver> Drivers { get; }

    IProductRepository Products { get; }

    IOrderRepository Orders { get; }

    IDeliveryBatchRepository DeliveryBatches { get; }

    IWarehouseItemRepository WarehouseItems { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    Task CommitAsync(CancellationToken cancellationToken = default);

    Task RollbackAsync(CancellationToken cancellationToken = default);
}
