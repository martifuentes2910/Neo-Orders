using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NeoOrders.Domain.Orders;

namespace NeoOrders.Infrastructure.Data.Configurations;

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> entity)
    {
        // Antes venían de [StringLength] en Infrastructure.Models.Order; ahora viven aquí.
        entity.Property(o => o.ProductName).IsRequired().HasMaxLength(120);
        entity.Property(o => o.Estado).IsRequired().HasMaxLength(30);
        entity.Property(o => o.PrecioUnitario).HasPrecision(18, 2);
        entity.Property(o => o.Total).HasPrecision(18, 2);
        entity.Property(o => o.OrderNumber).HasMaxLength(50);
        entity.HasIndex(o => o.OrderNumber).IsUnique().HasFilter("[OrderNumber] IS NOT NULL");
        entity.HasOne(o => o.Client)
              .WithMany(c => c.Orders)
              .HasForeignKey(o => o.ClientId)
              .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(o => o.Seller)
              .WithMany(s => s.Orders)
              .HasForeignKey(o => o.SellerId)
              .OnDelete(DeleteBehavior.SetNull);
        entity.HasOne(o => o.Product)
              .WithMany()
              .HasForeignKey(o => o.ProductId)
              .OnDelete(DeleteBehavior.SetNull);
    }
}
