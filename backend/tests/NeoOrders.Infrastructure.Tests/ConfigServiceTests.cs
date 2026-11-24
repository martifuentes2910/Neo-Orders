using Microsoft.EntityFrameworkCore;
using NeoOrders.Infrastructure.Data;
using NeoOrders.Infrastructure.Models;
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
        db.Clients.Add(new Client { Id = 1, Nombre = "C", Email = "c@c.com" });
        db.Sellers.Add(new Seller { Id = 1, Name = "S", Email = "s@s.com", Active = true });
        db.Products.Add(new Product { Id = 1, Sku = "SKU1", Name = "P", Category = "Cat", BasePrice = 10m, Stock = 1, Active = true });
        db.Orders.Add(new Order { Id = 1, ClientId = 1, ProductId = 1, ProductName = "P", Cantidad = 1, PrecioUnitario = 10m, Total = 10m, Estado = OrderStatuses.Pendiente });
        db.Notifications.Add(new Notification { Id = 1, Tipo = "sistema", Accion = "creado", Titulo = "T", Mensaje = "n" });
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
