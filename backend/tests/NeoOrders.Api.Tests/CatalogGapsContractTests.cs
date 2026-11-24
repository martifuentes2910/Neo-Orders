using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace NeoOrders.Api.Tests;

/// <summary>
/// Smoke tests de contrato para los gaps de catálogo no cubiertos por
/// <see cref="CatalogContractTests"/> / <see cref="CsvImportContractTests"/>:
/// plantillas CSV de clientes y pedidos, ajuste de precios, filtro de vendedores
/// activos y listado de categorías. Cada request con auth usa una factory
/// derivada con su propia base InMemory aislada.
/// </summary>
public class CatalogGapsContractTests : IClassFixture<NeoOrdersApiFactory>
{
    private readonly NeoOrdersApiFactory _factory;

    public CatalogGapsContractTests(NeoOrdersApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetClientsImportTemplate_ConAdmin_Retorna200Csv()
    {
        using var auth = _factory.CreateClientWithTestAuth(roles: ["Admin"]);

        var response = await auth.Client.GetAsync("/api/clients/import-csv/template");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("text/csv", response.Content.Headers.ContentType?.ToString(), StringComparison.OrdinalIgnoreCase);
        var body = await response.Content.ReadAsStringAsync();
        Assert.StartsWith("Name;Email;Phone;Address", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetOrdersImportTemplate_ConAdmin_Retorna200Csv()
    {
        using var auth = _factory.CreateClientWithTestAuth(roles: ["Admin"]);

        var response = await auth.Client.GetAsync("/api/orders/import-csv/template");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("text/csv", response.Content.Headers.ContentType?.ToString(), StringComparison.OrdinalIgnoreCase);
        var body = await response.Content.ReadAsStringAsync();
        Assert.StartsWith("OrderNumber;ClientId;SellerId;ProductId;Cantidad", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PostProductsPriceAdjustmentPreview_ConAdmin_Retorna200()
    {
        using var auth = _factory.CreateClientWithTestAuth(roles: ["Admin"]);
        var suffix = Guid.NewGuid().ToString("N");
        await SeedProductsAsync(auth, suffix, activeBasePrice: 10.00m, inactiveBasePrice: 50.00m);

        var response = await auth.Client.PostAsJsonAsync("/api/products/price-adjustment/preview", new { percent = 10m });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = await ReadJsonAsync(response);
        var root = doc.RootElement;
        Assert.Equal(JsonValueKind.Object, root.ValueKind);
        Assert.Equal(10m, GetProperty(root, "percent").GetDecimal());
        Assert.Equal(1.1m, GetProperty(root, "factor").GetDecimal());
        Assert.Equal(1, GetProperty(root, "affectedCount").GetInt32());
        Assert.Equal(1, GetProperty(root, "skippedInactiveCount").GetInt32());

        var items = GetProperty(root, "items");
        Assert.Equal(JsonValueKind.Array, items.ValueKind);
        var item = Assert.Single(items.EnumerateArray());
        Assert.Equal(10.00m, GetProperty(item, "currentBasePrice").GetDecimal());
        Assert.Equal(11.00m, GetProperty(item, "newBasePrice").GetDecimal());
        Assert.Empty(GetProperty(root, "errors").EnumerateArray());
    }

    /// <summary>
    /// Contrato real de preview con factor &lt;= 0: responde 200 con errors[]
    /// (a diferencia de apply, que mapea la misma guardia a 400 {message}).
    /// La vista previa no modifica precios.
    /// </summary>
    [Theory]
    [InlineData(-100)]
    [InlineData(-150)]
    public async Task PostProductsPriceAdjustmentPreview_FactorNoPositivo_Retorna200ConErrors(int percent)
    {
        using var auth = _factory.CreateClientWithTestAuth(roles: ["Admin"]);
        var suffix = Guid.NewGuid().ToString("N");
        var (activeId, inactiveId) = await SeedProductsAsync(auth, suffix, activeBasePrice: 10.00m, inactiveBasePrice: 50.00m);

        var response = await auth.Client.PostAsJsonAsync("/api/products/price-adjustment/preview", new { percent });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = await ReadJsonAsync(response);
        var root = doc.RootElement;
        Assert.Equal((decimal)percent, GetProperty(root, "percent").GetDecimal());
        Assert.True(GetProperty(root, "factor").GetDecimal() <= 0);
        Assert.Equal(0, GetProperty(root, "affectedCount").GetInt32());
        Assert.Empty(GetProperty(root, "items").EnumerateArray());

        var errors = GetProperty(root, "errors");
        var error = Assert.Single(errors.EnumerateArray());
        Assert.Equal("El factor resultante debe ser mayor a 0.", error.GetString());

        var prices = await auth.QueryDbAsync(async db =>
        {
            var active = await db.Products.SingleAsync(p => p.Id == activeId);
            var inactive = await db.Products.SingleAsync(p => p.Id == inactiveId);
            return (ActivePrice: active.BasePrice, InactivePrice: inactive.BasePrice);
        });
        Assert.Equal(10.00m, prices.ActivePrice);
        Assert.Equal(50.00m, prices.InactivePrice);
    }

    [Fact]
    public async Task PostProductsPriceAdjustmentApply_ConAdmin_Retorna200()
    {
        using var auth = _factory.CreateClientWithTestAuth(roles: ["Admin"]);
        var suffix = Guid.NewGuid().ToString("N");
        var (activeId, inactiveId) = await SeedProductsAsync(auth, suffix, activeBasePrice: 10.00m, inactiveBasePrice: 50.00m);

        var response = await auth.Client.PostAsJsonAsync("/api/products/price-adjustment/apply", new { percent = 10m });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = await ReadJsonAsync(response);
        var root = doc.RootElement;
        Assert.Equal(JsonValueKind.Object, root.ValueKind);
        Assert.Equal(1, GetProperty(root, "affectedCount").GetInt32());
        Assert.Equal(10m, GetProperty(root, "percent").GetDecimal());
        Assert.Equal(1.1m, GetProperty(root, "factor").GetDecimal());
        Assert.True(GetProperty(root, "appliedAtUtc").TryGetDateTime(out var appliedAtUtc));
        Assert.NotEqual(default, appliedAtUtc);

        var prices = await auth.QueryDbAsync(async db =>
        {
            var active = await db.Products.SingleAsync(p => p.Id == activeId);
            var inactive = await db.Products.SingleAsync(p => p.Id == inactiveId);
            return (ActivePrice: active.BasePrice, InactivePrice: inactive.BasePrice);
        });
        Assert.Equal(11.00m, prices.ActivePrice);
        Assert.Equal(50.00m, prices.InactivePrice);
    }

    [Fact]
    public async Task GetSellers_SoloActivosQuery_ConAuth_Retorna200Array()
    {
        using var auth = _factory.CreateClientWithTestAuth();
        var suffix = Guid.NewGuid().ToString("N");
        var (activeName, inactiveName) = await SeedSellersAsync(auth, suffix);

        var response = await auth.Client.GetAsync("/api/sellers?onlyActive=true");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = await ReadJsonAsync(response);
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
        var names = doc.RootElement.EnumerateArray()
            .Select(s => GetProperty(s, "name").GetString())
            .ToArray();
        Assert.Contains(activeName, names);
        Assert.DoesNotContain(inactiveName, names);
    }

    [Fact]
    public async Task GetProducts_Categories_ConAuth_Retorna200Array()
    {
        using var auth = _factory.CreateClientWithTestAuth();
        var suffix = Guid.NewGuid().ToString("N");
        var (firstCategory, secondCategory) = await SeedCategoriesAsync(auth, suffix);

        var response = await auth.Client.GetAsync("/api/products/categories");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = await ReadJsonAsync(response);
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
        var categories = doc.RootElement.EnumerateArray().Select(c => c.GetString()).ToArray();
        Assert.Contains(firstCategory, categories);
        Assert.Contains(secondCategory, categories);
    }

    [Fact]
    public async Task GetClients_ConAuth_Retorna200Array()
    {
        using var auth = _factory.CreateClientWithTestAuth();
        var suffix = Guid.NewGuid().ToString("N");
        var clientId = await SeedClientForListAsync(auth, suffix);

        var response = await auth.Client.GetAsync("/api/clients");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = await ReadJsonAsync(response);
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
        var client = Assert.Single(doc.RootElement.EnumerateArray(),
            c => GetProperty(c, "id").GetInt32() == clientId);
        Assert.Equal($"cliente.lista.{suffix}@test.local", GetProperty(client, "email").GetString());
        Assert.Equal(0, GetProperty(client, "ordersCount").GetInt32());
        Assert.Equal(0m, GetProperty(client, "totalSpent").GetDecimal());
    }

    [Fact]
    public async Task GetSellers_ConAuth_Retorna200ArrayIncluyeInactivos()
    {
        using var auth = _factory.CreateClientWithTestAuth();
        var suffix = Guid.NewGuid().ToString("N");
        var (activeName, inactiveName) = await SeedSellersAsync(auth, suffix);

        var response = await auth.Client.GetAsync("/api/sellers");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = await ReadJsonAsync(response);
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
        var names = doc.RootElement.EnumerateArray()
            .Select(s => GetProperty(s, "name").GetString())
            .ToArray();
        Assert.Contains(activeName, names);
        Assert.Contains(inactiveName, names);
    }

    private static Task<int> SeedClientForListAsync(TestAuthClient auth, string suffix) =>
        auth.QueryDbAsync(async db =>
        {
            var client = Client.Create($"Cliente lista {suffix}", $"cliente.lista.{suffix}@test.local");
            db.Clients.Add(client);
            await db.SaveChangesAsync();
            return client.Id;
        });

    private static async Task<(int ActiveId, int InactiveId)> SeedProductsAsync(
        TestAuthClient auth, string suffix, decimal activeBasePrice, decimal inactiveBasePrice)
    {
        return await auth.QueryDbAsync(async db =>
        {
            var active = Product.Create(
                sku: $"SKU-ADJ-ACTIVE-{suffix}",
                name: $"Activo {suffix}",
                category: "General",
                basePrice: activeBasePrice,
                cost: 5.00m,
                stock: 5,
                minStockAlert: 1,
                active: true);
            var inactive = Product.Create(
                sku: $"SKU-ADJ-INACTIVE-{suffix}",
                name: $"Inactivo {suffix}",
                category: "General",
                basePrice: inactiveBasePrice,
                cost: 25.00m,
                stock: 5,
                minStockAlert: 1,
                active: false);
            db.Products.AddRange(active, inactive);
            await db.SaveChangesAsync();
            return (active.Id, inactive.Id);
        });
    }

    private static async Task<(string ActiveName, string InactiveName)> SeedSellersAsync(TestAuthClient auth, string suffix)
    {
        var activeName = $"Vendedor activo {suffix}";
        var inactiveName = $"Vendedor inactivo {suffix}";
        return await auth.QueryDbAsync(async db =>
        {
            db.Sellers.AddRange(
                Seller.Create(activeName, $"seller.active.{suffix}@test.local", active: true),
                Seller.Create(inactiveName, $"seller.inactive.{suffix}@test.local", active: false));
            await db.SaveChangesAsync();
            return (activeName, inactiveName);
        });
    }

    private static async Task<(string FirstCategory, string SecondCategory)> SeedCategoriesAsync(TestAuthClient auth, string suffix)
    {
        var first = $"Categoría-A-{suffix}";
        var second = $"Categoría-B-{suffix}";
        return await auth.QueryDbAsync(async db =>
        {
            db.Products.AddRange(
                Product.Create($"SKU-CAT-A-{suffix}", $"Prod A {suffix}", first, 10m, 5m, 1, 1),
                Product.Create($"SKU-CAT-B-{suffix}", $"Prod B {suffix}", second, 20m, 10m, 1, 1));
            await db.SaveChangesAsync();
            return (first, second);
        });
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
