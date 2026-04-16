using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZimMarket.Domain.Entities.Orders;

namespace ZimMarket.Infrastructure.Persistence.Configurations;

public sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("order_items");

        builder.Property<Guid>("OrderId");
        builder.HasKey("OrderId", nameof(OrderItem.ProductId));

        builder.Property(x => x.ProductId).IsRequired();

        builder.Property(x => x.ProductTitle)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(x => x.Quantity).IsRequired();

        builder.OwnsOne(
            x => x.UnitPrice,
            money =>
            {
                money.Property(x => x.Amount).HasColumnName("unit_price_amount").HasPrecision(18, 2).IsRequired();
                money.Property(x => x.Currency).HasColumnName("unit_price_currency").IsRequired();
            });

        builder.Ignore(x => x.LineTotal);

        builder.HasIndex("OrderId");
        builder.HasIndex(x => x.ProductId);
    }
}
