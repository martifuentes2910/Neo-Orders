using System.Text;
using Microsoft.EntityFrameworkCore;
using NeoOrders.Domain.Orders;
using NeoOrders.Infrastructure.Data;
using NeoOrders.Infrastructure.Services;

namespace NeoOrders.Infrastructure.Tests;

public class CsvImportServiceTests
{
    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static MemoryStream Csv(string content) => new(Encoding.UTF8.GetBytes(content));

    [Fact]
    public async Task ImportClients_RejectsDuplicateEmailInFile()
    {
        using var db = CreateDb();
        var svc = new CsvImportService(db);
        var csv = Csv("Name;Email;Phone;Address\nA;a@a.com;;\nB;a@a.com;;\n");

        var result = await svc.ImportClientsAsync(csv);

        Assert.Equal(0, result.Imported);
        Assert.Contains(result.Errors, e => e.Field == "Email" && e.Message.Contains("duplicado"));
    }

    [Fact]
    public async Task ImportProducts_RejectsDuplicateSkuInFile()
    {
        using var db = CreateDb();
        var svc = new CsvImportService(db);
        var csv = Csv("Sku;Name;Category;BasePrice;Cost;Stock;MinStockAlert;Active\nSKU1;A;Cat;10;5;1;1;true\nSKU1;B;Cat;10;5;1;1;true\n");

        var result = await svc.ImportProductsAsync(csv);

        Assert.Equal(0, result.Imported);
        Assert.Contains(result.Errors, e => e.Field == "Sku");
    }

    [Fact]
    public async Task ImportOrders_RejectsMissingOrderNumber_AndCreatesPendiente()
    {
        using var db = CreateDb();
        db.Clients.Add(Client.Rehydrate(id: 1, nombre: "C", email: "c@c.com"));
        db.Sellers.Add(Seller.Rehydrate(id: 1, name: "S", email: "s@s.com", active: true));
        db.Products.Add(Product.Rehydrate(id: 1, sku: "SKU1", name: "Prod", category: "Cat", basePrice: 10m, stock: 5, active: true));
        await db.SaveChangesAsync();

        var svc = new CsvImportService(db);
        var bad = Csv("OrderNumber;ClientId;SellerId;ProductId;Cantidad\n;1;1;1;2\n");
        var badResult = await svc.ImportOrdersAsync(bad);
        Assert.Equal(0, badResult.Imported);
        Assert.Contains(badResult.Errors, e => e.Field == "OrderNumber");

        var ok = Csv("OrderNumber;ClientId;SellerId;ProductId;Cantidad\nORD-1;1;1;1;2\n");
        var okResult = await svc.ImportOrdersAsync(ok);
        Assert.Equal(1, okResult.Imported);
        var order = await db.Orders.SingleAsync();
        Assert.Equal(OrderStatusPolicy.Pendiente, order.Estado);
        Assert.Equal(5, (await db.Products.FindAsync(1))!.Stock); // no descuenta
    }

    [Fact]
    public async Task ImportOrders_RejectsInactiveProductOrSeller()
    {
        using var db = CreateDb();
        db.Clients.Add(Client.Rehydrate(id: 1, nombre: "C", email: "c@c.com"));
        db.Sellers.Add(Seller.Rehydrate(id: 1, name: "S", email: "s@s.com", active: false));
        db.Products.Add(Product.Rehydrate(id: 1, sku: "SKU1", name: "Prod", category: "Cat", basePrice: 10m, stock: 5, active: false));
        await db.SaveChangesAsync();

        var svc = new CsvImportService(db);
        var csv = Csv("OrderNumber;ClientId;SellerId;ProductId;Cantidad\nORD-1;1;1;1;2\n");
        var result = await svc.ImportOrdersAsync(csv);

        Assert.Equal(0, result.Imported);
        Assert.Contains(result.Errors, e => e.Field == "SellerId");
        Assert.Contains(result.Errors, e => e.Field == "ProductId");
    }

    [Fact]
    public async Task ImportOrders_RejectsDuplicateOrderNumberInFileAndDb()
    {
        using var db = CreateDb();
        db.Clients.Add(Client.Rehydrate(id: 1, nombre: "C", email: "c@c.com"));
        db.Products.Add(Product.Rehydrate(id: 1, sku: "SKU1", name: "Prod", category: "Cat", basePrice: 10m, stock: 5, active: true));
        db.Orders.Add(Order.Rehydrate(0, 1, null, 1, "Prod", 1, 10m, 10m, DateTime.UtcNow, OrderStatusPolicy.Pendiente, "ORD-EXIST"));
        await db.SaveChangesAsync();

        var svc = new CsvImportService(db);
        var csv = Csv("OrderNumber;ClientId;SellerId;ProductId;Cantidad\nORD-EXIST;1;;1;1\nORD-NEW;1;;1;1\nORD-NEW;1;;1;1\n");
        var result = await svc.ImportOrdersAsync(csv);

        Assert.Equal(0, result.Imported);
        Assert.Contains(result.Errors, e => e.Field == "OrderNumber");
        Assert.True(result.Errors.Count >= 1);
    }
}
