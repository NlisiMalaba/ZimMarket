using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZimMarket.Domain.Entities.Orders;
using ZimMarket.Domain.ValueObjects;

namespace ZimMarket.Infrastructure.Persistence.Configurations;

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.CustomerId).IsRequired();
        builder.Property(x => x.Status).IsRequired();
        builder.Property(x => x.PaymentStatus).IsRequired();

        builder.Property(x => x.PaymentReference).HasMaxLength(200);
        builder.Property(x => x.PaymentGatewayReference).HasMaxLength(500);
        builder.Property(x => x.FailedGatewayPaymentReference).HasMaxLength(200);
        builder.Property(x => x.InitiatedPaymentMethod);
        builder.Property(x => x.CancellationReason).HasMaxLength(1000);
        builder.Property(x => x.DeliveryPhotoKey).HasMaxLength(512);
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();

        builder.OwnsOne(
            x => x.DeliveryAddress,
            address =>
            {
                address.Property(x => x.Street).HasColumnName("delivery_street")
                    .HasMaxLength(Address.MaxStreetLength)
                    .IsRequired();
                address.Property(x => x.Suburb).HasColumnName("delivery_suburb")
                    .HasMaxLength(Address.MaxSuburbLength)
                    .IsRequired();
                address.Property(x => x.City).HasColumnName("delivery_city")
                    .HasMaxLength(Address.MaxCityLength)
                    .IsRequired();
                address.Property(x => x.Country).HasColumnName("delivery_country")
                    .HasMaxLength(Address.MaxCountryLength)
                    .IsRequired();
            });

        builder.OwnsOne(
            x => x.TotalAmount,
            money =>
            {
                money.Property(x => x.Amount).HasColumnName("total_amount").HasPrecision(18, 2).IsRequired();
                money.Property(x => x.Currency).HasColumnName("total_currency").IsRequired();
            });

        builder.HasMany(x => x.Items)
            .WithOne()
            .HasForeignKey("OrderId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Items)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(x => new { x.Status, x.CreatedAt });
        builder.HasIndex(x => x.CustomerId);
    }
}
