using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZimMarket.Domain.Entities.Users;
using ZimMarket.Domain.Enums;

namespace ZimMarket.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Email)
            .HasMaxLength(MappingConstants.EmailMaxLength)
            .IsRequired();

        builder.Property(x => x.FullName)
            .HasMaxLength(MappingConstants.FullNameMaxLength)
            .IsRequired();

        builder.Property(x => x.PasswordHash)
            .HasMaxLength(MappingConstants.PasswordHashMaxLength)
            .IsRequired();

        builder.Property(x => x.RefreshTokenHash)
            .HasMaxLength(MappingConstants.PasswordHashMaxLength);

        builder.Property(x => x.Role).IsRequired();
        builder.Property(x => x.KycStatus).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();

        builder.OwnsOne(
            x => x.PhoneNumber,
            phone =>
            {
                phone.Property(p => p.Value)
                    .HasColumnName("phone_number")
                    .HasMaxLength(MappingConstants.PhoneMaxLength)
                    .IsRequired();
                phone.HasIndex(p => p.Value).IsUnique();
            });

        builder.HasIndex(x => x.Email).IsUnique();

        builder.HasDiscriminator<UserRole>("user_type")
            .HasValue<Customer>(UserRole.Customer)
            .HasValue<Seller>(UserRole.Seller)
            .HasValue<Driver>(UserRole.Driver)
            .HasValue<AdminUser>(UserRole.Admin)
            .HasValue<SuperAdminUser>(UserRole.SuperAdmin);
    }
}
