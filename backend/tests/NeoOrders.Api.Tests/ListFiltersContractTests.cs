using System.Net;
using System.Text.Json;
using NeoOrders.Domain.Orders;

namespace NeoOrders.Api.Tests;

/// <summary>
/// Cobertura de los filtros de listado expuestos por la API (KAN-41/KAN-42 y
/// KAN-39 B4): no sólo la forma de la respuesta, sino que `search`, `category`,
/// `onlyInStock`, `estado`, `clientId`, `sellerId`, `productId`, `desde`, `hasta`,
/// `sku` y `nombre` acoten realmente los resultados. Cada cliente usa una DB
/// InMemory aislada.
/// </summary>
public class ListFiltersContractTests : IClassFixture<NeoOrdersApiFactory>
{
    private readonly NeoOrdersApiFactory _factory;

    public ListFiltersContractTests(NeoOrdersApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetClients_Search_FiltraPorNombreOCorreo()
    {
        using var auth = _factory.CreateClientWithTestAuth();
        var suffix = Guid.NewGuid().ToString("N");
        var (matchId, otherId) = await auth.QueryDbAsync(async db =>
        {
            var match = Client.Create($"Cliente buscado {suffix}", $"buscado.{suffix}@test.local");
            var other = Client.Create($"Cliente otro {Guid.NewGuid():N}", $"otro.{Guid.NewGuid():N}@test.local");
            db.Clients.AddRange(match, other);
            await db.SaveChangesAsync();
            return (match.Id, other.Id);
        });

        var response = await auth.Client.GetAsync($"/api/clients?search={suffix}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var ids = await ReadIdsAsync(response);
        Assert.Contains(matchId, ids);
        Assert.DoesNotContain(otherId, ids);
    }

    [Fact]
    public async Task GetProducts_SearchYCategoria_FiltranSoloCoincidencias()
    {
        using var auth = _factory.CreateClientWithTestAuth();
        var suffix = Guid.NewGuid().ToString("N");
        var targetCategory = $"Cat-{suffix}";
        var (matchId, otherId) = await auth.QueryDbAsync(async db =>
        {
            var match = NewProduct($"SKU-{suffix}", $"Producto {suffix}", targetCategory, 5);
            var other = NewProduct($"OTRO-{suffix}", $"Otro {suffix}", "General", 5);
            db.Products.AddRange(match, other);
            await db.SaveChangesAsync();
            return (match.Id, other.Id);
        });

        var response = await auth.Client.GetAsync(
            $"/api/products?search={suffix}&category={Uri.EscapeDataString(targetCategory)}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var ids = await ReadIdsAsync(response);
        Assert.Equal(new[] { matchId }, ids);
        Assert.DoesNotContain(otherId, ids);
    }

    [Fact]
    public async Task GetProducts_OnlyInStock_ExcluyeProductosSinStock()
    {
        using var auth = _factory.CreateClientWithTestAuth();
        var suffix = Guid.NewGuid().ToString("N");
        var targetCategory = $"Stock-{suffix}";
        var (withStockId, withoutStockId) = await auth.QueryDbAsync(async db =>
        {
            var withStock = NewProduct($"CON-{suffix}", $"Con stock {suffix}", targetCategory, 3);
            var withoutStock = NewProduct($"SIN-{suffix}", $"Sin stock {suffix}", targetCategory, 0);
            db.Products.AddRange(withStock, withoutStock);
            await db.SaveChangesAsync();
            return (withStock.Id, withoutStock.Id);
        });

        var response = await auth.Client.GetAsync(
            $"/api/products?onlyInStock=true&category={Uri.EscapeDataString(targetCategory)}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var ids = await ReadIdsAsync(response);
        Assert.Equal(new[] { withStockId }, ids);
        Assert.DoesNotContain(withoutStockId, ids);
    }

    [Fact]
    public async Task GetProducts_SkuYNombre_FiltranSoloCoincidencias()
    {
        using var auth = _factory.CreateClientWithTestAuth();
        var suffix = Guid.NewGuid().ToString("N");
        var category = $"Cat-{suffix}";
        var ids = await auth.QueryDbAsync(async db =>
        {
            var match = NewProduct($"SKU-{suffix}", $"Producto {suffix}", category, 5);
            var otroSku = NewProduct($"OTRO-{suffix}", $"Producto {suffix}", category, 5);
            var otroNombre = NewProduct($"SKU-{suffix}", $"Otro {suffix}", category, 5);
            var sinStock = NewProduct($"SKU-{suffix}-SIN", $"Producto {suffix} sin stock", category, 0);
            db.Products.AddRange(match, otroSku, otroNombre, sinStock);
            await db.SaveChangesAsync();
            return (Match: match.Id, OtroSku: otroSku.Id, OtroNombre: otroNombre.Id, SinStock: sinStock.Id);
        });

        var bySku = await ReadIdsAsync(await auth.Client.GetAsync($"/api/products?sku=SKU-{suffix}"));
        Assert.Contains(ids.Match, bySku);
        Assert.Contains(ids.OtroNombre, bySku);
        Assert.Contains(ids.SinStock, bySku);
        Assert.DoesNotContain(ids.OtroSku, bySku);

        // Mismo criterio case-insensitive que `search`.
        var bySkuUpper = await ReadIdsAsync(await auth.Client.GetAsync(
            $"/api/products?sku={Uri.EscapeDataString($"SKU-{suffix}".ToUpperInvariant())}"));
        Assert.Equal(bySku.Order(), bySkuUpper.Order());

        var byNombre = await ReadIdsAsync(await auth.Client.GetAsync(
            $"/api/products?nombre={Uri.EscapeDataString($"producto {suffix}")}"));
        Assert.Contains(ids.Match, byNombre);
        Assert.Contains(ids.OtroSku, byNombre);
        Assert.Contains(ids.SinStock, byNombre);
        Assert.DoesNotContain(ids.OtroNombre, byNombre);

        // Los nuevos filtros se combinan con los existentes (category + onlyInStock).
        var combined = await ReadIdsAsync(await auth.Client.GetAsync(
            $"/api/products?sku=SKU-{suffix}&nombre={Uri.EscapeDataString($"Producto {suffix}")}" +
            $"&onlyInStock=true&category={Uri.EscapeDataString(category)}"));
        Assert.Equal(new[] { ids.Match }, combined);
    }

    [Fact]
    public async Task GetOrders_ProductId_FiltraExactoYSeCombinaConFiltrosExistentes()
    {
        using var auth = _factory.CreateClientWithTestAuth();
        var suffix = Guid.NewGuid().ToString("N");
        var ids = await auth.QueryDbAsync(async db =>
        {
            var client = Client.Create($"Cliente {suffix}", $"prod.{suffix}@test.local");
            var seller = Seller.Create($"Vendedor {suffix}", $"v.{suffix}@test.local");
            db.AddRange(client, seller);
            await db.SaveChangesAsync();

            var first = NewProduct($"SKU-A-{suffix}", $"Producto A {suffix}", "General", 5);
            var second = NewProduct($"SKU-B-{suffix}", $"Producto B {suffix}", "General", 5);
            db.Products.AddRange(first, second);
            await db.SaveChangesAsync();

            var orderA = NewOrder(client.Id, seller.Id, first.Name, OrderStatusPolicy.Pendiente,
                $"A-{suffix}", productId: first.Id);
            var orderB = NewOrder(client.Id, seller.Id, second.Name, OrderStatusPolicy.Procesado,
                $"B-{suffix}", productId: second.Id);
            var sinProducto = NewOrder(client.Id, seller.Id, "Sin producto", OrderStatusPolicy.Pendiente,
                $"C-{suffix}");
            db.Orders.AddRange(orderA, orderB, sinProducto);
            await db.SaveChangesAsync();

            return (OrderA: orderA.Id, OrderB: orderB.Id, SinProducto: sinProducto.Id,
                ProductA: first.Id, Cliente: client.Id);
        });

        var byProduct = await ReadIdsAsync(await auth.Client.GetAsync($"/api/orders?productId={ids.ProductA}"));
        Assert.Equal(new[] { ids.OrderA }, byProduct);
        Assert.Empty(await ReadIdsAsync(await auth.Client.GetAsync("/api/orders?productId=2147483647")));

        var combined = await ReadIdsAsync(await auth.Client.GetAsync(
            $"/api/orders?productId={ids.ProductA}&estado={OrderStatusPolicy.Pendiente}&clientId={ids.Cliente}"));
        Assert.Equal(new[] { ids.OrderA }, combined);

        var conflicting = await ReadIdsAsync(await auth.Client.GetAsync(
            $"/api/orders?productId={ids.ProductA}&estado={OrderStatusPolicy.Procesado}"));
        Assert.Empty(conflicting);
    }

    [Fact]
    public async Task GetOrders_DesdeHasta_DateOnly_IncluyeElDiaCompleto()
    {
        using var auth = _factory.CreateClientWithTestAuth();
        var suffix = Guid.NewGuid().ToString("N");
        var ids = await auth.QueryDbAsync(async db =>
        {
            var client = Client.Create($"Cliente {suffix}", $"dates.{suffix}@test.local");
            db.Clients.Add(client);
            await db.SaveChangesAsync();

            var inicio = NewOrder(client.Id, null, "Inicio", OrderStatusPolicy.Pendiente, $"start-{suffix}",
                fecha: new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc));
            var mediodia = NewOrder(client.Id, null, "Mediodía", OrderStatusPolicy.Pendiente, $"mid-{suffix}",
                fecha: new DateTime(2026, 11, 1, 12, 34, 56, DateTimeKind.Utc));
            var fin = NewOrder(client.Id, null, "Fin", OrderStatusPolicy.Pendiente, $"end-{suffix}",
                fecha: new DateTime(2026, 11, 1, 23, 59, 59, DateTimeKind.Utc));
            var anterior = NewOrder(client.Id, null, "Anterior", OrderStatusPolicy.Pendiente, $"before-{suffix}",
                fecha: new DateTime(2026, 10, 31, 23, 59, 59, DateTimeKind.Utc));
            var posterior = NewOrder(client.Id, null, "Posterior", OrderStatusPolicy.Pendiente, $"after-{suffix}",
                fecha: new DateTime(2026, 11, 2, 0, 0, 0, DateTimeKind.Utc));
            db.Orders.AddRange(inicio, mediodia, fin, anterior, posterior);
            await db.SaveChangesAsync();
            return (Inicio: inicio.Id, Mediodia: mediodia.Id, Fin: fin.Id,
                Anterior: anterior.Id, Posterior: posterior.Id);
        });

        var mismoDia = await ReadIdsAsync(await auth.Client.GetAsync("/api/orders?desde=2026-11-01&hasta=2026-11-01"));
        Assert.Equal(new[] { ids.Inicio, ids.Mediodia, ids.Fin }.Order(), mismoDia.Order());
        Assert.DoesNotContain(ids.Anterior, mismoDia);
        Assert.DoesNotContain(ids.Posterior, mismoDia);

        var soloDesde = await ReadIdsAsync(await auth.Client.GetAsync("/api/orders?desde=2026-11-01"));
        Assert.Contains(ids.Posterior, soloDesde);
        Assert.DoesNotContain(ids.Anterior, soloDesde);

        var soloHasta = await ReadIdsAsync(await auth.Client.GetAsync("/api/orders?hasta=2026-10-31"));
        Assert.Equal(new[] { ids.Anterior }, soloHasta);
    }

    [Fact]
    public async Task GetOrders_EstadoClienteVendedor_FiltranSoloCoincidencias()
    {
        using var auth = _factory.CreateClientWithTestAuth();
        var suffix = Guid.NewGuid().ToString("N");
        var (firstId, secondId, firstClientId, secondClientId, firstSellerId, secondSellerId) =
            await auth.QueryDbAsync(async db =>
            {
                var firstClient = Client.Create($"Cliente A {suffix}", $"a.{suffix}@test.local");
                var secondClient = Client.Create($"Cliente B {suffix}", $"b.{suffix}@test.local");
                var firstSeller = Seller.Create($"Vendedor A {suffix}", $"va.{suffix}@test.local");
                var secondSeller = Seller.Create($"Vendedor B {suffix}", $"vb.{suffix}@test.local");
                db.AddRange(firstClient, secondClient, firstSeller, secondSeller);
                await db.SaveChangesAsync();

                var first = NewOrder(firstClient.Id, firstSeller.Id, $"Pedido A {suffix}", OrderStatusPolicy.Pendiente, $"A-{suffix}");
                var second = NewOrder(secondClient.Id, secondSeller.Id, $"Pedido B {suffix}", OrderStatusPolicy.Procesado, $"B-{suffix}");
                db.Orders.AddRange(first, second);
                await db.SaveChangesAsync();

                return (first.Id, second.Id, firstClient.Id, secondClient.Id, firstSeller.Id, secondSeller.Id);
            });

        var bySearch = await ReadIdsAsync(await auth.Client.GetAsync($"/api/orders?search={suffix}"));
        Assert.Equal(2, bySearch.Count);
        Assert.Contains(firstId, bySearch);
        Assert.Contains(secondId, bySearch);

        var byEstado = await ReadIdsAsync(await auth.Client.GetAsync($"/api/orders?estado={OrderStatusPolicy.Pendiente}"));
        Assert.Equal(new[] { firstId }, byEstado);

        var byCliente = await ReadIdsAsync(await auth.Client.GetAsync($"/api/orders?clientId={secondClientId}"));
        Assert.Equal(new[] { secondId }, byCliente);

        var byVendedor = await ReadIdsAsync(await auth.Client.GetAsync($"/api/orders?sellerId={firstSellerId}"));
        Assert.Equal(new[] { firstId }, byVendedor);

        var byOtroVendedor = await ReadIdsAsync(await auth.Client.GetAsync($"/api/orders?sellerId={secondSellerId}"));
        Assert.Equal(new[] { secondId }, byOtroVendedor);
    }

    private static Product NewProduct(string sku, string name, string category, int stock) =>
        Product.Create(sku, name, category, basePrice: 10m, cost: 5m, stock: stock);

    private static Order NewOrder(
        int clientId,
        int? sellerId,
        string productName,
        string estado,
        string orderNumber,
        int? productId = null,
        DateTime? fecha = null) =>
        Order.Rehydrate(
            id: 0,
            clientId: clientId,
            sellerId: sellerId,
            productId: productId,
            productName: productName,
            cantidad: 1,
            precioUnitario: 10m,
            total: 10m,
            fecha: fecha ?? DateTime.UtcNow,
            estado: estado,
            orderNumber: orderNumber);

    private static async Task<List<int>> ReadIdsAsync(HttpResponseMessage response)
    {
        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
        return doc.RootElement.EnumerateArray()
            .Select(item => GetProperty(item, "id").GetInt32())
            .ToList();
    }

    private static JsonElement GetProperty(JsonElement obj, string name)
    {
        if (obj.TryGetProperty(name, out var exact)) return exact;
        foreach (var property in obj.EnumerateObject())
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
                return property.Value;
        throw new Xunit.Sdk.XunitException($"La respuesta JSON no contiene la propiedad '{name}'.");
    }
}
