using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace NeoOrders.Api.Tests;

/// <summary>
/// Contratos de creación (201 Created) para el catálogo mínimo: clientes,
/// vendedores y productos. Cada test usa su propio cliente autenticado con rol
/// Admin y, por lo tanto, su propia base InMemory aislada.
/// </summary>
public class CatalogCreateContractTests : IClassFixture<NeoOrdersApiFactory>
{
    private readonly NeoOrdersApiFactory _factory;

    public CatalogCreateContractTests(NeoOrdersApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PostClient_ConAdmin_DatosValidos_Retorna201()
    {
        using var auth = _factory.CreateClientWithTestAuth(roles: ["Admin"]);
        var payload = new
        {
            nombre = "Cliente Contrato",
            email = "cliente.contrato@test.local",
            telefono = "123456789",
            direccion = "Calle Contrato 1"
        };

        var response = await auth.Client.PostAsJsonAsync("/api/clients", payload);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var doc = await ReadJsonAsync(response);
        var root = doc.RootElement;
        Assert.True(GetProperty(root, "id").GetInt32() > 0);
        Assert.Equal(payload.nombre, GetProperty(root, "nombre").GetString());
        Assert.Equal(payload.email, GetProperty(root, "email").GetString());
        Assert.Contains("/api/clients/", response.Headers.Location?.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task PostSeller_ConAdmin_DatosValidos_Retorna201()
    {
        using var auth = _factory.CreateClientWithTestAuth(roles: ["Admin"]);
        var payload = new
        {
            name = "Vendedor Contrato",
            email = "vendedor.contrato@test.local",
            phone = "987654321",
            commissionPercentage = 5m,
            active = true
        };

        var response = await auth.Client.PostAsJsonAsync("/api/sellers", payload);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var doc = await ReadJsonAsync(response);
        var root = doc.RootElement;
        Assert.True(GetProperty(root, "id").GetInt32() > 0);
        Assert.Equal(payload.name, GetProperty(root, "name").GetString());
        Assert.Equal(payload.email, GetProperty(root, "email").GetString());
        Assert.Contains("/api/sellers/", response.Headers.Location?.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task PostProduct_ConAdmin_DatosValidos_Retorna201()
    {
        using var auth = _factory.CreateClientWithTestAuth(roles: ["Admin"]);
        var payload = new
        {
            sku = "SKU-CREATE-CONTRACT",
            name = "Producto Contrato",
            category = "General",
            basePrice = 12.50m,
            cost = 7.25m,
            stock = 4,
            minStockAlert = 1,
            active = true
        };

        var response = await auth.Client.PostAsJsonAsync("/api/products", payload);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var doc = await ReadJsonAsync(response);
        var root = doc.RootElement;
        Assert.True(GetProperty(root, "id").GetInt32() > 0);
        Assert.Equal(payload.sku, GetProperty(root, "sku").GetString());
        Assert.Equal(payload.name, GetProperty(root, "name").GetString());
        Assert.Contains("/api/products/", response.Headers.Location?.ToString(), StringComparison.Ordinal);
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
