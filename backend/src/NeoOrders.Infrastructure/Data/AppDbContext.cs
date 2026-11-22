using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NeoOrders.Infrastructure.Models;

namespace NeoOrders.Infrastructure.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Seller> Sellers => Set<Seller>();
    public DbSet<CompanySettings> CompanySettings => Set<CompanySettings>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Order>(entity =>
        {
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
        });

        modelBuilder.Entity<Client>(entity =>
        {
            entity.HasIndex(c => c.Email).IsUnique();
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasIndex(p => p.Sku).IsUnique();
            entity.Property(p => p.BasePrice).HasPrecision(18, 2);
            entity.Property(p => p.Cost).HasPrecision(18, 2);
            entity.Property(p => p.ImageUrl).HasMaxLength(500);
            entity.Property(p => p.ImagePublicId).HasMaxLength(200);
        });

        modelBuilder.Entity<Seller>(entity =>
        {
            entity.HasIndex(s => s.Email).IsUnique();
            entity.Property(s => s.CommissionPercentage).HasPrecision(5, 2);
        });

        modelBuilder.Entity<CompanySettings>(entity =>
        {
            entity.Property(c => c.TipoDeCambio).HasPrecision(18, 4);
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasIndex(n => n.Fecha);
        });
    }
}
