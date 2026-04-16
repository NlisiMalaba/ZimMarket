using Microsoft.EntityFrameworkCore.Storage;
using ZimMarket.Domain.Entities.Users;
using ZimMarket.Domain.Interfaces;
using ZimMarket.Domain.Interfaces.Repositories;
using ZimMarket.Infrastructure.Persistence.Repositories;

namespace ZimMarket.Infrastructure.Persistence;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _dbContext;
    private IDbContextTransaction? _transaction;

    private IUserRepository<Customer>? _customers;
    private IUserRepository<Seller>? _sellers;
    private IUserRepository<Driver>? _drivers;
    private IProductRepository? _products;
    private IOrderRepository? _orders;
    private IDeliveryBatchRepository? _deliveryBatches;
    private IWarehouseItemRepository? _warehouseItems;

    public UnitOfWork(AppDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public IUserRepository<Customer> Customers => _customers ??= new UserRepository<Customer>(_dbContext);

    public IUserRepository<Seller> Sellers => _sellers ??= new UserRepository<Seller>(_dbContext);

    public IUserRepository<Driver> Drivers => _drivers ??= new UserRepository<Driver>(_dbContext);

    public IProductRepository Products => _products ??= new ProductRepository(_dbContext);

    public IOrderRepository Orders => _orders ??= new OrderRepository(_dbContext);

    public IDeliveryBatchRepository DeliveryBatches => _deliveryBatches ??= new DeliveryBatchRepository(_dbContext);

    public IWarehouseItemRepository WarehouseItems => _warehouseItems ??= new WarehouseItemRepository(_dbContext);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is not null)
            return;

        _transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null)
            throw new InvalidOperationException("Transaction has not been started.");

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            await _transaction.CommitAsync(cancellationToken);
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null)
            return;

        try
        {
            await _transaction.RollbackAsync(cancellationToken);
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }
}
