using Microsoft.EntityFrameworkCore;
using NeoOrders.Domain.Orders;
using NeoOrders.Infrastructure.Data;
using NeoOrders.Infrastructure.Services;

namespace NeoOrders.Infrastructure.Tests;

public class OrderServiceTests
{
    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task CreateAsync_CreatesPendingOrder_WithoutDecreasingStock()
    {
        using var db = CreateDb();
        db.Clients.Add(Client.Rehydrate(id: 1, nombre: "C", email: "c@c.com"));
        db.Products.Add(Product.Rehydrate(id: 1, sku: "SKU1", name: "Prod", category: "Cat", basePrice: 10m, stock: 5, active: true));
        await db.SaveChangesAsync();

        var svc = new OrderService(db);
        var result = await svc.CreateAsync(1, null, 1, 2, "ORD-1");

        Assert.True(result.Success);
        var order = await db.Orders.SingleAsync();
        Assert.Equal(OrderStatusPolicy.Pendiente, order.Estado);
        Assert.Equal("ORD-1", order.OrderNumber);
        Assert.Equal(5, (await db.Products.FindAsync(1))!.Stock);
    }

    [Fact]
    public async Task CreateAsync_RejectsDuplicateOrderNumber()
    {
        using var db = CreateDb();
        db.Clients.Add(Client.Rehydrate(id: 1, nombre: "C", email: "c@c.com"));
        db.Products.Add(Product.Rehydrate(id: 1, sku: "SKU1", name: "Prod", category: "Cat", basePrice: 10m, stock: 5, active: true));
        db.Orders.Add(Order.Rehydrate(0, 1, null, 1, "Prod", 1, 10m, 10m, DateTime.UtcNow, OrderStatusPolicy.Pendiente, "ORD-1"));
        await db.SaveChangesAsync();

        var svc = new OrderService(db);
        var result = await svc.CreateAsync(1, null, 1, 1, "ORD-1");

        Assert.False(result.Success);
        Assert.Contains("OrderNumber ya existe", result.Error);
    }

    [Fact]
    public async Task UpdateStatus_PendienteToProcesado_DecreasesStock()
    {
        using var db = CreateDb();
        db.Clients.Add(Client.Rehydrate(id: 1, nombre: "C", email: "c@c.com"));
        db.Products.Add(Product.Rehydrate(id: 1, sku: "SKU1", name: "Prod", category: "Cat", basePrice: 10m, stock: 5, active: true));
        db.Orders.Add(Order.Rehydrate(1, 1, null, 1, "Prod", 2, 10m, 20m, DateTime.UtcNow, OrderStatusPolicy.Pendiente, null));
        await db.SaveChangesAsync();

        var svc = new OrderService(db);
        var result = await svc.UpdateStatusAsync(1, OrderStatusPolicy.Procesado);

        Assert.True(result.Success);
        Assert.Equal(3, (await db.Products.FindAsync(1))!.Stock);
        Assert.Equal(OrderStatusPolicy.Procesado, (await db.Orders.FindAsync(1))!.Estado);
    }

    [Fact]
    public async Task UpdateStatus_ProcesadoToCancelado_RefundsStock()
    {
        using var db = CreateDb();
        db.Products.Add(Product.Rehydrate(id: 1, sku: "SKU1", name: "Prod", category: "Cat", basePrice: 10m, stock: 3, active: true));
        db.Orders.Add(Order.Rehydrate(1, 1, null, 1, "Prod", 2, 10m, 20m, DateTime.UtcNow, OrderStatusPolicy.Procesado, null));
        await db.SaveChangesAsync();

        var svc = new OrderService(db);
        var result = await svc.UpdateStatusAsync(1, OrderStatusPolicy.Cancelado);

        Assert.True(result.Success);
        Assert.Equal(5, (await db.Products.FindAsync(1))!.Stock);
    }

    [Fact]
    public async Task UpdateStatus_InvalidTransition_ReturnsError()
    {
        using var db = CreateDb();
        db.Orders.Add(Order.Rehydrate(1, 1, null, 1, "Prod", 1, 10m, 10m, DateTime.UtcNow, OrderStatusPolicy.Pendiente, null));
        await db.SaveChangesAsync();

        var svc = new OrderService(db);
        var result = await svc.UpdateStatusAsync(1, OrderStatusPolicy.Entregado);

        Assert.False(result.Success);
        Assert.Contains("no permitida", result.Error, StringComparison.OrdinalIgnoreCase);
    }
}
