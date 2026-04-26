using Microsoft.EntityFrameworkCore;
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
    private IUserRepository<AdminUser>? _admins;
    private IUserRepository<SuperAdminUser>? _superAdmins;
    private IDriverReadRepository? _driverRead;
    private IProductRepository? _products;
    private ICategoryRepository? _categories;
    private IOrderRepository? _orders;
    private IPaymentIdempotencyRepository? _paymentIdempotency;
    private IDeliveryBatchRepository? _deliveryBatches;
    private IWarehouseItemRepository? _warehouseItems;
    private IPendingKycReadRepository? _pendingKyc;
    private IDashboardStatsReadRepository? _dashboardStats;
    private IDriverLocationRepository? _driverLocations;
    private IAuthTokenRepository? _authTokens;
    private IAdminApprovalStateRepository? _adminApprovalStates;

    public UnitOfWork(AppDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public IUserRepository<Customer> Customers => _customers ??= new UserRepository<Customer>(_dbContext);

    public IUserRepository<Seller> Sellers => _sellers ??= new UserRepository<Seller>(_dbContext);

    public IUserRepository<Driver> Drivers => _drivers ??= new UserRepository<Driver>(_dbContext);

    public IUserRepository<AdminUser> Admins => _admins ??= new UserRepository<AdminUser>(_dbContext);

    public IUserRepository<SuperAdminUser> SuperAdmins => _superAdmins ??= new UserRepository<SuperAdminUser>(_dbContext);

    public IDriverReadRepository DriverRead => _driverRead ??= new DriverReadRepository(_dbContext);

    public IProductRepository Products => _products ??= new ProductRepository(_dbContext);

    public ICategoryRepository Categories => _categories ??= new CategoryRepository(_dbContext);

    public IOrderRepository Orders => _orders ??= new OrderRepository(_dbContext);

    public IPaymentIdempotencyRepository PaymentIdempotency =>
        _paymentIdempotency ??= new PaymentIdempotencyRepository(_dbContext);

    public IDeliveryBatchRepository DeliveryBatches => _deliveryBatches ??= new DeliveryBatchRepository(_dbContext);

    public IWarehouseItemRepository WarehouseItems => _warehouseItems ??= new WarehouseItemRepository(_dbContext);

    public IPendingKycReadRepository PendingKyc => _pendingKyc ??= new PendingKycReadRepository(_dbContext);

    public IDashboardStatsReadRepository DashboardStats =>
        _dashboardStats ??= new DashboardStatsReadRepository(_dbContext);

    public IDriverLocationRepository DriverLocations =>
        _driverLocations ??= new DriverLocationRepository(_dbContext);

    public IAuthTokenRepository AuthTokens =>
        _authTokens ??= new AuthTokenRepository(_dbContext);

    public IAdminApprovalStateRepository AdminApprovalStates =>
        _adminApprovalStates ??= new AdminApprovalStateRepository(_dbContext);

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

    public async Task<T> RunInTransactionAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        IExecutionStrategy strategy = _dbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(
            async ct =>
            {
                await BeginTransactionAsync(ct).ConfigureAwait(false);
                try
                {
                    T result = await operation().ConfigureAwait(false);
                    await CommitAsync(ct).ConfigureAwait(false);
                    return result;
                }
                catch
                {
                    await RollbackAsync(ct).ConfigureAwait(false);
                    throw;
                }
            },
            cancellationToken).ConfigureAwait(false);
    }
}
