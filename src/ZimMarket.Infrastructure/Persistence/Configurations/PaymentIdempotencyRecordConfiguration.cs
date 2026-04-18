using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZimMarket.Domain.Entities.Payments;

namespace ZimMarket.Infrastructure.Persistence.Configurations;

public sealed class PaymentIdempotencyRecordConfiguration : IEntityTypeConfiguration<PaymentIdempotencyRecord>
{
    public void Configure(EntityTypeBuilder<PaymentIdempotencyRecord> builder)
    {
        builder.ToTable("payment_idempotency_records");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.IdempotencyKey).HasMaxLength(128).IsRequired();
        builder.Property(x => x.OrderId).IsRequired();
        builder.Property(x => x.CustomerId).IsRequired();
        builder.Property(x => x.GatewayReference).HasMaxLength(500).IsRequired();
        builder.Property(x => x.PaymentUrl).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.PaymentMethod).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();

        builder.HasIndex(x => x.IdempotencyKey).IsUnique();
        builder.HasIndex(x => x.OrderId);
    }
}
