using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NeoOrders.Domain.Sellers;

namespace NeoOrders.Infrastructure.Data.Configurations;

public sealed class SellerConfiguration : IEntityTypeConfiguration<Seller>
{
    public void Configure(EntityTypeBuilder<Seller> entity)
    {
        entity.Property(s => s.Name).IsRequired().HasMaxLength(100);
        entity.Property(s => s.Email).HasMaxLength(100);
        entity.Property(s => s.Phone).HasMaxLength(30);
        entity.Property(s => s.CommissionPercentage).HasPrecision(5, 2);
        entity.HasIndex(s => s.Email).IsUnique();
    }
}
