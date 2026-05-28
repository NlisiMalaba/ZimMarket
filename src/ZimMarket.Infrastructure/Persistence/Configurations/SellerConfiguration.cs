using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZimMarket.Domain.Entities.Users;
using ZimMarket.Domain.ValueObjects;

namespace ZimMarket.Infrastructure.Persistence.Configurations;

public sealed class SellerConfiguration : IEntityTypeConfiguration<Seller>
{
    public void Configure(EntityTypeBuilder<Seller> builder)
    {
        builder.Property(x => x.BusinessName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.ProfilePhotoKey)
            .HasColumnName("profile_photo_key")
            .HasMaxLength(MappingConstants.DocumentKeyMaxLength);

        builder.OwnsOne(
            x => x.DefaultPickupAddress,
            address =>
            {
                address.Property(x => x.Street).HasColumnName("default_pickup_street")
                    .HasMaxLength(Address.MaxStreetLength);
                address.Property(x => x.Suburb).HasColumnName("default_pickup_suburb")
                    .HasMaxLength(Address.MaxSuburbLength);
                address.Property(x => x.City).HasColumnName("default_pickup_city")
                    .HasMaxLength(Address.MaxCityLength);
                address.Property(x => x.Country).HasColumnName("default_pickup_country")
                    .HasMaxLength(Address.MaxCountryLength);
            });

        builder.Property(x => x.NationalIdDocumentKey)
            .HasMaxLength(MappingConstants.DocumentKeyMaxLength)
            .IsRequired();

        builder.Property(x => x.ProofOfResidenceDocumentKey)
            .HasMaxLength(MappingConstants.DocumentKeyMaxLength)
            .IsRequired();

        builder.Property(x => x.RejectionReason).HasMaxLength(1000);
        builder.Property(x => x.IsApproved).IsRequired();

        builder.HasIndex(x => x.BusinessName);
    }
}
