using Microsoft.EntityFrameworkCore;
using NeoOrders.Domain.Orders;
using NeoOrders.Infrastructure.Data;
using NeoOrders.Infrastructure.Models;
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
        db.Clients.Add(new Client { Id = 1, Nombre = "C", Email = "c@c.com" });
        db.Products.Add(new Product { Id = 1, Sku = "SKU1", Name = "Prod", Category = "Cat", BasePrice = 10m, Stock = 5, Active = true });
        await db.SaveChangesAsync();

        var svc = new OrderService(db);
        var result = await svc.CreateAsync(1, null, 1, 2, "ORD-1");

        Assert.True(result.Success);
        var order = await db.Orders.SingleAsync();
        Assert.Equal(OrderStatuses.Pendiente, order.Estado);
        Assert.Equal("ORD-1", order.OrderNumber);
        Assert.Equal(5, (await db.Products.FindAsync(1))!.Stock);
    }

    [Fact]
    public async Task CreateAsync_RejectsDuplicateOrderNumber()
    {
        using var db = CreateDb();
        db.Clients.Add(new Client { Id = 1, Nombre = "C", Email = "c@c.com" });
        db.Products.Add(new Product { Id = 1, Sku = "SKU1", Name = "Prod", Category = "Cat", BasePrice = 10m, Stock = 5, Active = true });
        db.Orders.Add(new Order { ClientId = 1, ProductId = 1, ProductName = "Prod", Cantidad = 1, PrecioUnitario = 10m, Total = 10m, Estado = OrderStatuses.Pendiente, OrderNumber = "ORD-1" });
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
        db.Clients.Add(new Client { Id = 1, Nombre = "C", Email = "c@c.com" });
        db.Products.Add(new Product { Id = 1, Sku = "SKU1", Name = "Prod", Category = "Cat", BasePrice = 10m, Stock = 5, Active = true });
        db.Orders.Add(new Order { Id = 1, ClientId = 1, ProductId = 1, ProductName = "Prod", Cantidad = 2, PrecioUnitario = 10m, Total = 20m, Estado = OrderStatuses.Pendiente });
        await db.SaveChangesAsync();

        var svc = new OrderService(db);
        var result = await svc.UpdateStatusAsync(1, OrderStatuses.Procesado);

        Assert.True(result.Success);
        Assert.Equal(3, (await db.Products.FindAsync(1))!.Stock);
        Assert.Equal(OrderStatuses.Procesado, (await db.Orders.FindAsync(1))!.Estado);
    }

    [Fact]
    public async Task UpdateStatus_ProcesadoToCancelado_RefundsStock()
    {
        using var db = CreateDb();
        db.Products.Add(new Product { Id = 1, Sku = "SKU1", Name = "Prod", Category = "Cat", BasePrice = 10m, Stock = 3, Active = true });
        db.Orders.Add(new Order { Id = 1, ClientId = 1, ProductId = 1, ProductName = "Prod", Cantidad = 2, PrecioUnitario = 10m, Total = 20m, Estado = OrderStatuses.Procesado });
        await db.SaveChangesAsync();

        var svc = new OrderService(db);
        var result = await svc.UpdateStatusAsync(1, OrderStatuses.Cancelado);

        Assert.True(result.Success);
        Assert.Equal(5, (await db.Products.FindAsync(1))!.Stock);
    }

    [Fact]
    public async Task UpdateStatus_InvalidTransition_ReturnsError()
    {
        using var db = CreateDb();
        db.Orders.Add(new Order { Id = 1, ClientId = 1, ProductId = 1, ProductName = "Prod", Cantidad = 1, PrecioUnitario = 10m, Total = 10m, Estado = OrderStatuses.Pendiente });
        await db.SaveChangesAsync();

        var svc = new OrderService(db);
        var result = await svc.UpdateStatusAsync(1, OrderStatuses.Entregado);

        Assert.False(result.Success);
        Assert.Contains("Transición de estado no permitida", result.Error);
    }
}
