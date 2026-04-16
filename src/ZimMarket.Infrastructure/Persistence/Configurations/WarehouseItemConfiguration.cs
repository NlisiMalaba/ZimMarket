using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZimMarket.Domain.Entities.Warehouse;

namespace ZimMarket.Infrastructure.Persistence.Configurations;

public sealed class WarehouseItemConfiguration : IEntityTypeConfiguration<WarehouseItem>
{
    public void Configure(EntityTypeBuilder<WarehouseItem> builder)
    {
        builder.ToTable("warehouse_items");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.OrderId).IsRequired();
        builder.Property(x => x.ProductId).IsRequired();
        builder.Property(x => x.ArrivedAt).IsRequired();
        builder.Property(x => x.QcStatus).IsRequired();
        builder.Property(x => x.QcNotes).HasMaxLength(1000);
        builder.Property(x => x.PackagedAt);
        builder.Property(x => x.BatchId);
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();

        builder.HasIndex(x => x.OrderId);
        builder.HasIndex(x => x.ProductId);
        builder.HasIndex(x => x.BatchId);
        builder.HasIndex(x => new { x.QcStatus, x.ArrivedAt });
    }
}
