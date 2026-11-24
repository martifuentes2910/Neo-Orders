using Microsoft.EntityFrameworkCore;
using NeoOrders.Infrastructure.Data;
using NeoOrders.Infrastructure.Services;
using NeoOrders.Infrastructure.Storage;

namespace NeoOrders.Infrastructure.Tests;

/// <summary>
/// KAN-39 B4: sku y nombre acotan el listado con el mismo criterio "contains"
/// case-insensitive que `search`, y se combinan con category/onlyInStock.
/// </summary>
public class ProductServiceFilterTests
{
    private static (AppDbContext Db, ProductService Service) CreateSut()
    {
        var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
        return (db, new ProductService(db, new UnconfiguredProductImageStorage()));
    }

    private static Product NewProduct(string sku, string name, string category = "General", int stock = 5) =>
        Product.Create(sku, name, category, basePrice: 10m, cost: 5m, stock: stock);

    [Fact]
    public async Task ListAsync_SkuYNombre_ContainsCaseInsensitive()
    {
        var (db, sut) = CreateSut();
        using (db)
        {
            var laptopGamer = NewProduct("SKU-ABC-1", "Laptop Gamer");
            var laptopOficina = NewProduct("OTR-ABC-1", "Laptop Oficina");
            var mouse = NewProduct("SKU-XYZ-1", "Mouse");
            db.Products.AddRange(laptopGamer, laptopOficina, mouse);
            await db.SaveChangesAsync();

            var bySku = await sut.ListAsync(null, null, null, sku: "sku-abc");
            Assert.Equal(new[] { laptopGamer.Id }, bySku.Select(p => p.Id));

            var byNombre = await sut.ListAsync(null, null, null, nombre: "laptop");
            Assert.Equal(2, byNombre.Count);
            Assert.Contains(laptopGamer.Id, byNombre.Select(p => p.Id));
            Assert.Contains(laptopOficina.Id, byNombre.Select(p => p.Id));

            var ambos = await sut.ListAsync(null, null, null, sku: "SKU-ABC", nombre: "gamer");
            Assert.Equal(new[] { laptopGamer.Id }, ambos.Select(p => p.Id));

            var sinCoincidencias = await sut.ListAsync(null, null, null, sku: "SKU-ABC", nombre: "mouse");
            Assert.Empty(sinCoincidencias);
        }
    }

    [Fact]
    public async Task ListAsync_SkuNombre_CombinanConSearchCategoriaYOnlyInStock()
    {
        var (db, sut) = CreateSut();
        using (db)
        {
            var target = NewProduct("SKU-ABC-1", "Laptop Gamer", "Tec", stock: 5);
            var sinStock = NewProduct("SKU-ABC-2", "Laptop Outlet", "Tec", stock: 0);
            var otraCategoria = NewProduct("SKU-ABC-3", "Laptop Otra", "Hogar", stock: 5);
            db.Products.AddRange(target, sinStock, otraCategoria);
            await db.SaveChangesAsync();

            var filtrosExistentes = await sut.ListAsync(
                search: "laptop", category: "Tec", onlyInStock: true);
            Assert.Equal(new[] { target.Id }, filtrosExistentes.Select(p => p.Id));

            var todosLosFiltros = await sut.ListAsync(
                search: "laptop", category: "Tec", onlyInStock: true,
                sku: "sku-abc", nombre: "GAMER");
            Assert.Equal(new[] { target.Id }, todosLosFiltros.Select(p => p.Id));

            // "Todos" en categoría sigue siendo un comodín, igual que en el FE.
            var categoriaTodos = await sut.ListAsync(
                null, "Todos", null, sku: "SKU-ABC", nombre: "gamer");
            Assert.Equal(new[] { target.Id }, categoriaTodos.Select(p => p.Id));
        }
    }
}
