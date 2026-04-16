using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZimMarket.Domain.Entities.Logistics;

namespace ZimMarket.Infrastructure.Persistence.Configurations;

public sealed class DeliveryBatchConfiguration : IEntityTypeConfiguration<DeliveryBatch>
{
    public void Configure(EntityTypeBuilder<DeliveryBatch> builder)
    {
        builder.ToTable("delivery_batches");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.DriverId).IsRequired();
        builder.Property(x => x.WarehouseId).IsRequired();
        builder.Property(x => x.Status).IsRequired();
        builder.Property(x => x.CollectedAt);
        builder.Property(x => x.CompletedAt);
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();

        builder.Property(x => x.OrderIds)
            .HasColumnName("order_ids")
            .HasConversion(
                x => JsonSerializer.Serialize(x, (JsonSerializerOptions?)null),
                x => JsonSerializer.Deserialize<List<Guid>>(x, (JsonSerializerOptions?)null) ?? new List<Guid>())
            .Metadata.SetValueComparer(
                new ValueComparer<IReadOnlyList<Guid>>(
                    (a, b) => a != null && b != null && a.SequenceEqual(b),
                    x => x.Aggregate(0, (hash, item) => HashCode.Combine(hash, item.GetHashCode())),
                    x => x.ToList()));

        builder.HasIndex(x => x.DriverId);
        builder.HasIndex(x => x.WarehouseId);
        builder.HasIndex(x => new { x.Status, x.CreatedAt });
    }
}
