using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZimMarket.Domain.Entities.Users;

namespace ZimMarket.Infrastructure.Persistence.Configurations;

public sealed class SellerConfiguration : IEntityTypeConfiguration<Seller>
{
    public void Configure(EntityTypeBuilder<Seller> builder)
    {
        builder.Property(x => x.BusinessName)
            .HasMaxLength(200)
            .IsRequired();

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
