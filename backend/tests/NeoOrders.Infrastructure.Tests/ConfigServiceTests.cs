using Microsoft.EntityFrameworkCore;
using NeoOrders.Infrastructure.Data;
using NeoOrders.Infrastructure.Services;
using NeoOrders.Domain.Orders;

namespace NeoOrders.Infrastructure.Tests;

public class ConfigServiceTests
{
    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task ClearAsync_RemovesOperationalData_AndReturnsTypedCounts()
    {
        using var db = CreateDb();
        db.Clients.Add(Client.Rehydrate(id: 1, nombre: "C", email: "c@c.com"));
        db.Sellers.Add(Seller.Rehydrate(id: 1, name: "S", email: "s@s.com", active: true));
        db.Products.Add(Product.Rehydrate(id: 1, sku: "SKU1", name: "P", category: "Cat", basePrice: 10m, stock: 1, active: true));
        db.Orders.Add(Order.Rehydrate(1, 1, null, 1, "P", 1, 10m, 10m, DateTime.UtcNow, OrderStatusPolicy.Pendiente, null));
        db.Notifications.Add(Notification.Rehydrate(id: 1, tipo: "sistema", accion: "creado", titulo: "T", mensaje: "n"));
        await db.SaveChangesAsync();

        var svc = new ConfigService(db);
        var result = await svc.ClearAsync();

        Assert.Equal("Datos operativos eliminados", result.Message);
        Assert.Equal(1, result.Eliminados.Clientes);
        Assert.Equal(1, result.Eliminados.Vendedores);
        Assert.Equal(1, result.Eliminados.Productos);
        Assert.Equal(1, result.Eliminados.Pedidos);
        Assert.Equal(1, result.Eliminados.Notificaciones);
        Assert.Empty(db.Clients);
        Assert.Empty(db.Sellers);
        Assert.Empty(db.Products);
        Assert.Empty(db.Orders);
        Assert.Empty(db.Notifications);
    }
}
