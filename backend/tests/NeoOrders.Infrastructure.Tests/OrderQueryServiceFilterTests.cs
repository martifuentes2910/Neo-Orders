using Microsoft.EntityFrameworkCore;
using NeoOrders.Infrastructure.Data;
using NeoOrders.Infrastructure.Services;

namespace NeoOrders.Infrastructure.Tests;

/// <summary>
/// KAN-39 B4: semántica de los filtros nuevos del listado de pedidos a nivel
/// de consulta (sin pasar por HTTP): productId exacto y desde/hasta inclusivos,
/// con fechas del tipo yyyy-MM-dd y con timestamps ISO.
/// </summary>
public class OrderQueryServiceFilterTests
{
    private static (AppDbContext Db, OrderQueryService Service) CreateSut()
    {
        var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
        return (db, new OrderQueryService(db));
    }

    private static Order NewOrder(int id, int? productId, DateTime fecha, string estado = "Pendiente") =>
        Order.Rehydrate(id, 1, null, productId, $"Producto {id}", 1, 10m, 10m, fecha, estado, $"ORD-{id}");

    // Client es una navegación requerida y la consulta la incluye: sin la fila
    // principal, EF la descarta. Seller es opcional y no hace falta sembrarla.
    private static void SeedClient(AppDbContext db) =>
        db.Clients.Add(Client.Rehydrate(id: 1, nombre: "Cliente", email: "cliente@test.local"));

    [Fact]
    public async Task ListAsync_ProductId_FiltraExactoYComponerConFiltrosExistentes()
    {
        var (db, sut) = CreateSut();
        using (db)
        {
            SeedClient(db);
            db.Orders.AddRange(
                NewOrder(1, 10, new DateTime(2026, 11, 5, 10, 0, 0, DateTimeKind.Utc)),
                NewOrder(2, 20, new DateTime(2026, 11, 5, 10, 0, 0, DateTimeKind.Utc), estado: "Procesado"),
                NewOrder(3, null, new DateTime(2026, 11, 5, 10, 0, 0, DateTimeKind.Utc)));
            await db.SaveChangesAsync();

            var byProduct = await sut.ListAsync(null, null, null, null, productId: 10);
            Assert.Equal(new[] { 1 }, byProduct.Select(o => o.Id));

            var sinProducto = await sut.ListAsync(null, null, null, null, productId: 3);
            Assert.Empty(sinProducto);

            var combo = await sut.ListAsync(
                search: "Producto", estado: "Procesado", clientId: 1, sellerId: null,
                productId: 20, desde: "2026-11-05", hasta: "2026-11-05");
            Assert.Equal(new[] { 2 }, combo.Select(o => o.Id));

            var estadoDistinto = await sut.ListAsync(
                search: null, estado: "Procesado", clientId: null, sellerId: null, productId: 10);
            Assert.Empty(estadoDistinto);
        }
    }

    [Fact]
    public async Task ListAsync_DesdeHasta_DateOnly_IncluyeInicioYFinDelDia()
    {
        var (db, sut) = CreateSut();
        using (db)
        {
            SeedClient(db);
            db.Orders.AddRange(
                NewOrder(1, 1, new DateTime(2026, 11, 5, 0, 0, 0, DateTimeKind.Utc)),
                NewOrder(2, 1, new DateTime(2026, 11, 5, 12, 30, 0, DateTimeKind.Utc)),
                NewOrder(3, 1, new DateTime(2026, 11, 5, 23, 59, 59, DateTimeKind.Utc)),
                NewOrder(4, 1, new DateTime(2026, 11, 5, 23, 59, 59, DateTimeKind.Utc).AddTicks(9_999_999)),
                NewOrder(5, 1, new DateTime(2026, 11, 4, 23, 59, 59, DateTimeKind.Utc)),
                NewOrder(6, 1, new DateTime(2026, 11, 6, 0, 0, 0, DateTimeKind.Utc)));
            await db.SaveChangesAsync();

            var mismoDia = await sut.ListAsync(null, null, null, null, desde: "2026-11-05", hasta: "2026-11-05");
            Assert.Equal(new[] { 4, 3, 2, 1 }, mismoDia.Select(o => o.Id));

            var soloDesde = await sut.ListAsync(null, null, null, null, desde: "2026-11-06");
            Assert.Equal(new[] { 6 }, soloDesde.Select(o => o.Id));

            var soloHasta = await sut.ListAsync(null, null, null, null, hasta: "2026-11-04");
            Assert.Equal(new[] { 5 }, soloHasta.Select(o => o.Id));
        }
    }

    [Fact]
    public async Task ListAsync_DesdeHasta_TimestampIso_InclusivoYNormalizadoAUtc()
    {
        var (db, sut) = CreateSut();
        using (db)
        {
            SeedClient(db);
            db.Orders.AddRange(
                NewOrder(1, 1, new DateTime(2026, 11, 5, 11, 59, 59, DateTimeKind.Utc)),
                NewOrder(2, 1, new DateTime(2026, 11, 5, 12, 0, 0, DateTimeKind.Utc)),
                NewOrder(3, 1, new DateTime(2026, 11, 5, 12, 0, 1, DateTimeKind.Utc)));
            await db.SaveChangesAsync();

            // 09:00-03:00 == 12:00Z. Ambos extremos son inclusivos: entran las
            // filas exactamente en 12:00:00 y 12:00:01, no la de 11:59:59.
            var rango = await sut.ListAsync(
                null, null, null, null,
                desde: "2026-11-05T09:00:00-03:00", hasta: "2026-11-05T12:00:01Z");
            Assert.Equal(new[] { 2, 3 }, rango.Select(o => o.Id).Order());
        }
    }

    [Fact]
    public async Task ListAsync_DesdeHasta_ValorInvalido_SeIgnora()
    {
        var (db, sut) = CreateSut();
        using (db)
        {
            SeedClient(db);
            db.Orders.AddRange(
                NewOrder(1, 1, new DateTime(2026, 11, 5, 10, 0, 0, DateTimeKind.Utc)),
                NewOrder(2, 1, new DateTime(2020, 1, 1, 10, 0, 0, DateTimeKind.Utc)));
            await db.SaveChangesAsync();

            var todos = await sut.ListAsync(null, null, null, null, desde: "no-es-fecha", hasta: "");
            Assert.Equal(2, todos.Count);

            var soloInvalidoHasta = await sut.ListAsync(null, null, null, null, hasta: "tampoco-fecha");
            Assert.Equal(2, soloInvalidoHasta.Count);
        }
    }
}
