using MediatR;
using Microsoft.EntityFrameworkCore;
using ZimMarket.Domain.Entities;
using ZimMarket.Domain.Entities.Catalogue;
using ZimMarket.Domain.Entities.Logistics;
using ZimMarket.Domain.Entities.Orders;
using ZimMarket.Domain.Entities.Payments;
using ZimMarket.Domain.Entities.Users;
using ZimMarket.Domain.Entities.Warehouse;
using ZimMarket.Domain.Events;

namespace ZimMarket.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext
{
    private readonly IPublisher _publisher;

    public AppDbContext(DbContextOptions<AppDbContext> options, IPublisher publisher)
        : base(options)
    {
        _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
    }

    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<Seller> Sellers => Set<Seller>();

    public DbSet<Driver> Drivers => Set<Driver>();

    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();

    public DbSet<SuperAdminUser> SuperAdminUsers => Set<SuperAdminUser>();

    public DbSet<Product> Products => Set<Product>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Order> Orders => Set<Order>();

    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    public DbSet<PaymentIdempotencyRecord> PaymentIdempotencyRecords => Set<PaymentIdempotencyRecord>();

    public DbSet<DeliveryBatch> DeliveryBatches => Set<DeliveryBatch>();

    public DbSet<DriverLocation> DriverLocations => Set<DriverLocation>();

    public DbSet<WarehouseItem> WarehouseItems => Set<WarehouseItem>();

    public DbSet<ExchangeRate> ExchangeRates => Set<ExchangeRate>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SetAuditFields();
        List<IDomainEvent> domainEvents = CollectDomainEvents();

        int rowsAffected = await base.SaveChangesAsync(cancellationToken);

        foreach (IDomainEvent domainEvent in domainEvents)
        {
            await _publisher.Publish(domainEvent, cancellationToken);
        }

        return rowsAffected;
    }

    private void SetAuditFields()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Property(nameof(BaseEntity.CreatedAt)).CurrentValue = now;
                entry.Property(nameof(BaseEntity.UpdatedAt)).CurrentValue = now;
                continue;
            }

            if (entry.State == EntityState.Modified)
            {
                entry.Property(nameof(BaseEntity.CreatedAt)).IsModified = false;
                entry.Property(nameof(BaseEntity.UpdatedAt)).CurrentValue = now;
            }
        }
    }

    private List<IDomainEvent> CollectDomainEvents()
    {
        return ChangeTracker
            .Entries<BaseEntity>()
            .SelectMany(entry => entry.Entity.PopDomainEvents())
            .ToList();
    }
}
