using System.Text;
using Microsoft.EntityFrameworkCore;
using NeoOrders.Application.Contracts.Import;
using NeoOrders.Application.Contracts.Notifications;
using NeoOrders.Application.Contracts.Productos;
using NeoOrders.Application.DTOs.Notifications;
using NeoOrders.Infrastructure.Data;
using NeoOrders.Infrastructure.Services;

namespace NeoOrders.Infrastructure.Tests;

public class SqlServerSmokeTests
{
    private static string? GetCs() => Environment.GetEnvironmentVariable("SMOKE_SQLSERVER_CS");

    private static AppDbContext CreateDb(string cs)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(cs).Options;
        return new AppDbContext(options);
    }

    private sealed class NoopNotifications : INotificationService
    {
        public Task PushAsync(string tipo, string accion, string titulo, string mensaje, string? link = null, int? entidadId = null) => Task.CompletedTask;
        public Task<NotificationsResponseDto> GetRecentAsync(int take = 60) =>
            Task.FromResult(new NotificationsResponseDto { Items = new List<NotificationDto>(), Resumen = new NotificationsSummaryDto() });
    }

    [Fact]
    public async Task Smoke_SqlServer_KAN38A1()
    {
        var cs = GetCs();
        if (string.IsNullOrWhiteSpace(cs))
            return; // opt-in: SMOKE_SQLSERVER_CS

        var suffix = Guid.NewGuid().ToString("N")[..8];
        int clientId, sellerId, productId, orderId;

        await using (var db = CreateDb(cs))
        {
            var client = Client.Create($"Smoke Client {suffix}", $"smoke{suffix}@test.local", "123", "Dir");
            var seller = Seller.Create($"Smoke Seller {suffix}", $"seller{suffix}@test.local", "555", 5m, true);
            var product = Product.Create($"SMK-{suffix}", $"Smoke Product {suffix}", "Test", 100m, 50m, 10, 2, true);
            db.Clients.Add(client); db.Sellers.Add(seller); db.Products.Add(product);
            await db.SaveChangesAsync();
            clientId = client.Id; sellerId = seller.Id; productId = product.Id;
        }

        await using (var db = CreateDb(cs))
        {
            var svc = new OrderService(db);
            var created = await svc.CreateAsync(clientId, sellerId, productId, 2);
            Assert.True(created.Success, created.Error);
            orderId = created.OrderId!.Value;
            var order = await db.Orders.FindAsync(orderId);
            Assert.NotNull(order);
            Assert.Equal("Pendiente", order!.Estado);
            Assert.True(order.Total > 0);
        }

        await using (var db = CreateDb(cs))
        {
            var loaded = await db.Orders.Include(o => o.Client).Include(o => o.Seller).Include(o => o.Product).FirstAsync(o => o.Id == orderId);
            Assert.NotNull(loaded.Client);
            Assert.NotNull(loaded.Seller);
            Assert.NotNull(loaded.Product);
            Assert.Equal(clientId, loaded.Client!.Id);
        }

        await using (var db = CreateDb(cs))
        {
            var svc = new OrderService(db);
            var res = await svc.UpdateStatusAsync(orderId, "Procesado");
            Assert.True(res.Success, res.Error);
            var product = await db.Products.FindAsync(productId);
            Assert.Equal(8, product!.Stock);
        }

        await using (var db = CreateDb(cs))
        {
            var svc = new OrderService(db);
            var res = await svc.UpdateStatusAsync(orderId, "Cancelado");
            Assert.True(res.Success, res.Error);
            var product = await db.Products.FindAsync(productId);
            Assert.Equal(10, product!.Stock);
        }

        await using (var db = CreateDb(cs))
        {
            db.Clients.Add(Client.Create("Dup", $"smoke{suffix}@test.local", null, null));
            await Assert.ThrowsAnyAsync<DbUpdateException>(() => db.SaveChangesAsync());
        }

        await using (var db = CreateDb(cs))
        {
            var byStatus = await db.Orders.CountAsync(o => o.Estado == "Cancelado" && o.ClientId == clientId);
            Assert.True(byStatus >= 1);
        }

        await using (var db = CreateDb(cs))
        {
            var csvSvc = new CsvImportService(db);
            var csv = "Sku;Name;Category;BasePrice;Cost;Stock;MinStockAlert;Active\n" +
                      $"SMKCSV-{suffix};CSV Prod;Cat;10;5;3;1;true\n";
            using var ms = new MemoryStream(Encoding.UTF8.GetBytes(csv));
            var res = await csvSvc.ImportProductsAsync(ms);
            Assert.True(res.Imported == 1, $"Imported={res.Imported} Errors={string.Join("|", res.Errors.Select(e => $"{e.Row}:{e.Field}:{e.Message}"))}");
            Assert.Empty(res.Errors);
        }

        await using (var db = CreateDb(cs))
        {
            db.Orders.RemoveRange(await db.Orders.Where(o => o.ClientId == clientId).ToListAsync());
            db.Products.RemoveRange(await db.Products.Where(p => p.Sku.StartsWith($"SMKCSV-{suffix}")).ToListAsync());
            var p = await db.Products.FindAsync(productId); if (p != null) db.Products.Remove(p);
            var c = await db.Clients.FindAsync(clientId); if (c != null) db.Clients.Remove(c);
            var s = await db.Sellers.FindAsync(sellerId); if (s != null) db.Sellers.Remove(s);
            await db.SaveChangesAsync();
        }
    }
}



