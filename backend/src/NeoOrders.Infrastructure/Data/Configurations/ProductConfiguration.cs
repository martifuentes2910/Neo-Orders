using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NeoOrders.Domain.Products;

namespace NeoOrders.Infrastructure.Data.Configurations;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> entity)
    {
        entity.Property(p => p.Sku).IsRequired().HasMaxLength(50);
        entity.Property(p => p.Name).IsRequired().HasMaxLength(120);
        entity.Property(p => p.Category).IsRequired().HasMaxLength(60);
        entity.Property(p => p.BasePrice).HasPrecision(18, 2);
        entity.Property(p => p.Cost).HasPrecision(18, 2);
        entity.Property(p => p.ImageUrl).HasMaxLength(500);
        entity.Property(p => p.ImagePublicId).HasMaxLength(200);
        entity.HasIndex(p => p.Sku).IsUnique();
    }
}
