using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZimMarket.Domain.Entities;

namespace ZimMarket.Infrastructure.Persistence.Configurations;

public sealed class ExchangeRateConfiguration : IEntityTypeConfiguration<ExchangeRate>
{
    public void Configure(EntityTypeBuilder<ExchangeRate> builder)
    {
        builder.ToTable("exchange_rates");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.BaseCurrency)
            .HasMaxLength(MappingConstants.CurrencyCodeMaxLength)
            .IsRequired();

        builder.Property(x => x.QuoteCurrency)
            .HasMaxLength(MappingConstants.CurrencyCodeMaxLength)
            .IsRequired();

        builder.Property(x => x.Rate)
            .HasPrecision(18, 6)
            .IsRequired();

        builder.Property(x => x.EffectiveAt).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();

        builder.HasIndex(x => new { x.BaseCurrency, x.QuoteCurrency, x.EffectiveAt }).IsUnique();
    }
}
