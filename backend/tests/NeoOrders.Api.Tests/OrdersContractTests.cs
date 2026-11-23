using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using NeoOrders.Infrastructure.Models;

namespace NeoOrders.Api.Tests;

/// <summary>
/// Smoke/contract tests de /api/orders: flujos felices y errores de negocio
/// clave. Cada test usa su propio cliente autenticado y, por lo tanto, su
/// propia base InMemory aislada (Seed:RunOnStartup=false).
/// </summary>
public class OrdersContractTests : IClassFixture<NeoOrdersApiFactory>
{
    private readonly NeoOrdersApiFactory _factory;

    public OrdersContractTests(NeoOrdersApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PostOrder_DatosValidos_Retorna201ConId()
    {
        using var auth = _factory.CreateClientWithTestAuth();
        var (clientId, sellerId, productId) = await SeedCatalogAsync(auth);

        var response = await PostOrderAsync(auth.Client, clientId, sellerId, productId);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var doc = await ReadJsonAsync(response);
        Assert.True(GetProperty(doc.RootElement, "id").GetInt32() > 0);
    }

    [Fact]
    public async Task PostOrder_ClienteInvalido_Retorna400Message()
    {
        using var auth = _factory.CreateClientWithTestAuth();
        var productId = await SeedProductAsync(auth);

        var response = await PostOrderAsync(auth.Client, 999999, null, productId);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var doc = await ReadJsonAsync(response);
        Assert.Equal("Cliente no válido", GetProperty(doc.RootElement, "message").GetString());
    }

    [Fact]
    public async Task PostOrder_SinProducto_Retorna400Message()
    {
        using var auth = _factory.CreateClientWithTestAuth();
        var clientId = await SeedClientAsync(auth);

        var response = await PostOrderAsync(auth.Client, clientId, null, null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var doc = await ReadJsonAsync(response);
        Assert.Equal("Debe seleccionar un producto del catálogo", GetProperty(doc.RootElement, "message").GetString());
    }

    [Fact]
    public async Task PatchOrderStatus_TransicionValida_Retorna204()
    {
        using var auth = _factory.CreateClientWithTestAuth();
        var (clientId, sellerId, productId) = await SeedCatalogAsync(auth, productStock: 5);
        var orderId = await CreatePendingOrderIdAsync(auth, clientId, sellerId, productId);

        var response = await auth.Client.PatchAsJsonAsync(
            $"/api/orders/{orderId}/estado", new { estado = "Procesado" });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task PatchOrderStatus_TransicionInvalida_Retorna400Message()
    {
        using var auth = _factory.CreateClientWithTestAuth();
        var (clientId, sellerId, productId) = await SeedCatalogAsync(auth);
        var orderId = await CreatePendingOrderIdAsync(auth, clientId, sellerId, productId);

        var response = await auth.Client.PatchAsJsonAsync(
            $"/api/orders/{orderId}/estado", new { estado = "Entregado" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var doc = await ReadJsonAsync(response);
        var message = GetProperty(doc.RootElement, "message").GetString() ?? string.Empty;
        Assert.False(string.IsNullOrWhiteSpace(message));
        // El contrato real es "Transición de estado no permitida: Pendiente → Entregado".
        // Se asertan las partes ASCII para no acoplar el test a la codificación.
        Assert.StartsWith("Transici", message, StringComparison.Ordinal);
        Assert.Contains("Pendiente", message, StringComparison.Ordinal);
        Assert.Contains("Entregado", message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DeleteOrder_AdminExistente_Retorna204()
    {
        using var auth = _factory.CreateClientWithTestAuth(roles: ["Admin"]);
        var (clientId, sellerId, productId) = await SeedCatalogAsync(auth);
        var orderId = await CreatePendingOrderIdAsync(auth, clientId, sellerId, productId);

        var response = await auth.Client.DeleteAsync($"/api/orders/{orderId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task PutOrder_IdMismatch_Retorna400Message()
    {
        using var auth = _factory.CreateClientWithTestAuth();

        var response = await auth.Client.PutAsJsonAsync("/api/orders/1", new
        {
            id = 2,
            clientId = 1,
            sellerId = (int?)null,
            productId = (int?)null,
            productName = "Producto Contrato",
            cantidad = 1,
            precioUnitario = 10.50m,
            estado = "Pendiente"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var doc = await ReadJsonAsync(response);
        Assert.Equal("El ID de la ruta no coincide con el cuerpo.", GetProperty(doc.RootElement, "message").GetString());
    }

    private static async Task<(int ClientId, int SellerId, int ProductId)> SeedCatalogAsync(
        TestAuthClient auth, int productStock = 5)
    {
        var clientId = await SeedClientAsync(auth);
        var sellerId = await SeedSellerAsync(auth);
        var productId = await SeedProductAsync(auth, productStock);
        return (clientId, sellerId, productId);
    }

    private static Task<int> SeedClientAsync(TestAuthClient auth)
    {
        var suffix = Guid.NewGuid().ToString("N");
        return auth.QueryDbAsync(async db =>
        {
            var client = new Client
            {
                Nombre = $"Cliente Contrato {suffix}",
                Email = $"orders.contract.client.{suffix}@test.local"
            };
            db.Clients.Add(client);
            await db.SaveChangesAsync();
            return client.Id;
        });
    }

    private static Task<int> SeedSellerAsync(TestAuthClient auth)
    {
        var suffix = Guid.NewGuid().ToString("N");
        return auth.QueryDbAsync(async db =>
        {
            var seller = new Seller
            {
                Name = $"Vendedor Contrato {suffix}",
                Email = $"orders.contract.seller.{suffix}@test.local",
                Active = true
            };
            db.Sellers.Add(seller);
            await db.SaveChangesAsync();
            return seller.Id;
        });
    }

    private static Task<int> SeedProductAsync(TestAuthClient auth, int stock = 5)
    {
        var suffix = Guid.NewGuid().ToString("N");
        return auth.QueryDbAsync(async db =>
        {
            var product = new Product
            {
                Sku = $"SKU-ORDERS-CONTRACT-{suffix}",
                Name = $"Producto Contrato {suffix}",
                Category = "General",
                BasePrice = 10.50m,
                Cost = 5.25m,
                Stock = stock,
                MinStockAlert = 1,
                Active = true
            };
            db.Products.Add(product);
            await db.SaveChangesAsync();
            return product.Id;
        });
    }

    private static Task<HttpResponseMessage> PostOrderAsync(
        HttpClient client, int clientId, int? sellerId, int? productId, int cantidad = 1) =>
        client.PostAsJsonAsync("/api/orders", new
        {
            clientId,
            sellerId,
            productId,
            productName = "Producto Contrato",
            cantidad,
            precioUnitario = 10.50m
        });

    private static async Task<int> CreatePendingOrderIdAsync(
        TestAuthClient auth, int clientId, int? sellerId, int? productId)
    {
        var response = await PostOrderAsync(auth.Client, clientId, sellerId, productId);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var doc = await ReadJsonAsync(response);
        return GetProperty(doc.RootElement, "id").GetInt32();
    }

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response) =>
        await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());

    private static JsonElement GetProperty(JsonElement obj, string name)
    {
        if (obj.TryGetProperty(name, out var exact)) return exact;
        foreach (var property in obj.EnumerateObject())
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
                return property.Value;
        throw new Xunit.Sdk.XunitException($"La respuesta JSON no contiene la propiedad '{name}'.");
    }
}
