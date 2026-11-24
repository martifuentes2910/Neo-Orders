using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NeoOrders.Domain.Clients;
using NeoOrders.Domain.Config;
using NeoOrders.Domain.Notifications;
using NeoOrders.Domain.Orders;
using NeoOrders.Domain.Products;
using NeoOrders.Domain.Sellers;
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
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
