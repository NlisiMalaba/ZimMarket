using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZimMarket.Domain.Entities.Users;

namespace ZimMarket.Infrastructure.Persistence.Configurations;

public sealed class AdminApprovalStateConfiguration : IEntityTypeConfiguration<AdminApprovalState>
{
    public void Configure(EntityTypeBuilder<AdminApprovalState> builder)
    {
        builder.ToTable("admin_approval_states");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();

        builder.HasIndex(x => x.UserId).IsUnique();
    }
}
