using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZimMarket.Domain.Entities.Users;
using ZimMarket.Domain.ValueObjects;

namespace ZimMarket.Infrastructure.Persistence.Configurations;

public sealed class DriverConfiguration : IEntityTypeConfiguration<Driver>
{
    public void Configure(EntityTypeBuilder<Driver> builder)
    {
        builder.Property(x => x.LicenseNumber)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.LicenseDocumentKey)
            .HasMaxLength(MappingConstants.DocumentKeyMaxLength)
            .IsRequired();

        builder.Property(x => x.VehicleRegistration)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.VehicleDocumentKey)
            .HasMaxLength(MappingConstants.DocumentKeyMaxLength)
            .IsRequired();

        builder.Property(x => x.RejectionReason).HasMaxLength(1000);
        builder.Property(x => x.IsApproved).IsRequired();
        builder.Property(x => x.DriverStatus).IsRequired();

        builder.OwnsOne(
            x => x.LastKnownLocation,
            location =>
            {
                location.Property(x => x.Latitude).HasColumnName("last_known_latitude");
                location.Property(x => x.Longitude).HasColumnName("last_known_longitude");
                location.HasIndex(x => new { x.Latitude, x.Longitude });
            });

        builder.HasIndex(x => x.LicenseNumber).IsUnique();
        builder.HasIndex(x => x.VehicleRegistration).IsUnique();
    }
}
