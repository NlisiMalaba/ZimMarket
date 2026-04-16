using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZimMarket.Domain.Entities.Catalogue;
using ZimMarket.Domain.Enums;
using ZimMarket.Domain.ValueObjects;

namespace ZimMarket.Infrastructure.Persistence.Configurations;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.SellerId).IsRequired();
        builder.Property(x => x.CategoryId).IsRequired();
        builder.Property(x => x.StockQuantity).IsRequired();

        builder.Property(x => x.Title)
            .HasMaxLength(Product.MaxTitleLength)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(Product.MaxDescriptionLength)
            .IsRequired();

        builder.Property(x => x.Status).IsRequired();

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();

        builder.Property(x => x.ImageKeys)
            .HasColumnName("image_keys")
            .HasConversion(
                x => JsonSerializer.Serialize(x, (JsonSerializerOptions?)null),
                x => JsonSerializer.Deserialize<List<string>>(x, (JsonSerializerOptions?)null) ?? new List<string>())
            .Metadata.SetValueComparer(
                new ValueComparer<IReadOnlyList<string>>(
                    (a, b) => a != null && b != null && a.SequenceEqual(b),
                    x => x.Aggregate(0, (hash, item) => HashCode.Combine(hash, item.GetHashCode())),
                    x => x.ToList()));

        builder.OwnsOne(
            x => x.Price,
            money =>
            {
                money.Property(x => x.Amount).HasColumnName("price_amount").HasPrecision(18, 2).IsRequired();
                money.Property(x => x.Currency).HasColumnName("price_currency").IsRequired();
            });

        builder.OwnsOne(
            x => x.PickupAddress,
            address =>
            {
                address.Property(x => x.Street).HasColumnName("pickup_street")
                    .HasMaxLength(Address.MaxStreetLength)
                    .IsRequired();
                address.Property(x => x.Suburb).HasColumnName("pickup_suburb")
                    .HasMaxLength(Address.MaxSuburbLength)
                    .IsRequired();
                address.Property(x => x.City).HasColumnName("pickup_city")
                    .HasMaxLength(Address.MaxCityLength)
                    .IsRequired();
                address.Property(x => x.Country).HasColumnName("pickup_country")
                    .HasMaxLength(Address.MaxCountryLength)
                    .IsRequired();
            });

        builder.HasIndex(x => x.SellerId);
        builder.HasIndex(x => x.CategoryId);
        builder.HasIndex(x => new { x.Status, x.CreatedAt });
        builder.HasIndex(x => x.Title);

        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(x => x.Status != ProductStatus.Deleted);
    }
}
