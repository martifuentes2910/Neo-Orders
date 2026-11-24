using System.Net;
using System.Text.Json;
using NeoOrders.Domain.Orders;

namespace NeoOrders.Api.Tests;

/// <summary>
/// Cobertura de los filtros de listado ya expuestos por la API (KAN-41/KAN-42):
/// hasta ahora sólo se asertaba la forma de la respuesta, no que los filtros
/// `search`, `category`, `onlyInStock`, `estado`, `clientId` y `sellerId`
/// acotaran realmente los resultados. Cada cliente usa una DB InMemory aislada.
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

                var first = NewOrder(firstClient.Id, firstSeller.Id, $"Pedido A {suffix}", OrderStatuses.Pendiente, $"A-{suffix}");
                var second = NewOrder(secondClient.Id, secondSeller.Id, $"Pedido B {suffix}", OrderStatuses.Procesado, $"B-{suffix}");
                db.Orders.AddRange(first, second);
                await db.SaveChangesAsync();

                return (first.Id, second.Id, firstClient.Id, secondClient.Id, firstSeller.Id, secondSeller.Id);
            });

        var bySearch = await ReadIdsAsync(await auth.Client.GetAsync($"/api/orders?search={suffix}"));
        Assert.Equal(2, bySearch.Count);
        Assert.Contains(firstId, bySearch);
        Assert.Contains(secondId, bySearch);

        var byEstado = await ReadIdsAsync(await auth.Client.GetAsync($"/api/orders?estado={OrderStatuses.Pendiente}"));
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

    private static Order NewOrder(int clientId, int sellerId, string productName, string estado, string orderNumber) =>
        Order.Rehydrate(
            id: 0,
            clientId: clientId,
            sellerId: sellerId,
            productId: null,
            productName: productName,
            cantidad: 1,
            precioUnitario: 10m,
            total: 10m,
            fecha: DateTime.UtcNow,
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
