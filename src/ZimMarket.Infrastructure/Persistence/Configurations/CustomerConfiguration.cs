using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZimMarket.Domain.Entities.Users;
using ZimMarket.Domain.ValueObjects;

namespace ZimMarket.Infrastructure.Persistence.Configurations;

public sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.Property(x => x.PushNotificationToken).HasMaxLength(512);

        builder.OwnsMany(
            x => x.DeliveryAddresses,
            addressBuilder =>
            {
                addressBuilder.ToTable("customer_delivery_addresses");
                addressBuilder.WithOwner().HasForeignKey("customer_id");
                addressBuilder.Property(x => x.Id)
                    .HasColumnName("id")
                    .ValueGeneratedNever();
                // Shadow FK uses the column name from WithOwner().HasForeignKey; CLR key property is Id (column "id").
                addressBuilder.HasKey("customer_id", nameof(CustomerDeliveryAddress.Id));

                addressBuilder.OwnsOne(
                    x => x.Address,
                    ownedAddress =>
                    {
                        ownedAddress.Property(a => a.Street).HasColumnName("street")
                            .HasMaxLength(Address.MaxStreetLength)
                            .IsRequired();
                        ownedAddress.Property(a => a.Suburb).HasColumnName("suburb")
                            .HasMaxLength(Address.MaxSuburbLength)
                            .IsRequired();
                        ownedAddress.Property(a => a.City).HasColumnName("city")
                            .HasMaxLength(Address.MaxCityLength)
                            .IsRequired();
                        ownedAddress.Property(a => a.Country).HasColumnName("country")
                            .HasMaxLength(Address.MaxCountryLength)
                            .IsRequired();
                    });
            });
    }
}
