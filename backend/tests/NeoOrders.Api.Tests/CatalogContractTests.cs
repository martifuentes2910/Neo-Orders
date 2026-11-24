using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace NeoOrders.Api.Tests;

public class CatalogContractTests : IClassFixture<NeoOrdersApiFactory>
{
    private readonly NeoOrdersApiFactory _factory;

    public CatalogContractTests(NeoOrdersApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetProducts_SinAuth_Retorna401()
    {
        using var client = CreateAnonymousClient();
        var response = await client.GetAsync("/api/products");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostProduct_SinRolAdmin_Retorna403()
    {
        using var auth = _factory.CreateClientWithTestAuth();
        var response = await auth.Client.PostAsJsonAsync("/api/products", new { name = "P", sku = "S1", basePrice = 1m, cost = 1m, stock = 1 });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetProductById_Inexistente_Retorna404ConMessage()
    {
        using var auth = _factory.CreateClientWithTestAuth();
        var response = await auth.Client.GetAsync("/api/products/999999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        using var doc = await ReadJsonAsync(response);
        Assert.Equal("Producto no encontrado.", GetProperty(doc.RootElement, "message").GetString());
    }

    [Fact]
    public async Task DeleteProduct_Inexistente_Admin_Retorna404ConMessage()
    {
        using var auth = _factory.CreateClientWithTestAuth(roles: ["Admin"]);
        var response = await auth.Client.DeleteAsync("/api/products/999999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        using var doc = await ReadJsonAsync(response);
        Assert.Equal("Producto no encontrado.", GetProperty(doc.RootElement, "message").GetString());
    }

    [Fact]
    public async Task GetClients_SinAuth_Retorna401()
    {
        using var client = CreateAnonymousClient();
        var response = await client.GetAsync("/api/clients");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostClient_SinRolAdmin_Retorna403()
    {
        using var auth = _factory.CreateClientWithTestAuth();
        var response = await auth.Client.PostAsJsonAsync("/api/clients", new { nombre = "C", email = "c@test.local" });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetClientById_Inexistente_Retorna404ConMessage()
    {
        using var auth = _factory.CreateClientWithTestAuth();
        var response = await auth.Client.GetAsync("/api/clients/999999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        using var doc = await ReadJsonAsync(response);
        Assert.Equal("Cliente no encontrado.", GetProperty(doc.RootElement, "message").GetString());
    }

    [Fact]
    public async Task DeleteClient_Inexistente_Admin_Retorna404ConMessage()
    {
        using var auth = _factory.CreateClientWithTestAuth(roles: ["Admin"]);
        var response = await auth.Client.DeleteAsync("/api/clients/999999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        using var doc = await ReadJsonAsync(response);
        Assert.Equal("Cliente no encontrado.", GetProperty(doc.RootElement, "message").GetString());
    }

    [Fact]
    public async Task GetSellers_SinAuth_Retorna401()
    {
        using var client = CreateAnonymousClient();
        var response = await client.GetAsync("/api/sellers");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostSeller_SinRolAdmin_Retorna403()
    {
        using var auth = _factory.CreateClientWithTestAuth();
        var response = await auth.Client.PostAsJsonAsync("/api/sellers", new { name = "V", email = "v@test.local" });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetSellerById_Inexistente_Retorna404ConMessage()
    {
        using var auth = _factory.CreateClientWithTestAuth();
        var response = await auth.Client.GetAsync("/api/sellers/999999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        using var doc = await ReadJsonAsync(response);
        Assert.Equal("Vendedor no encontrado.", GetProperty(doc.RootElement, "message").GetString());
    }

    [Fact]
    public async Task DeleteSeller_Inexistente_Admin_Retorna404ConMessage()
    {
        using var auth = _factory.CreateClientWithTestAuth(roles: ["Admin"]);
        var response = await auth.Client.DeleteAsync("/api/sellers/999999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        using var doc = await ReadJsonAsync(response);
        Assert.Equal("Vendedor no encontrado.", GetProperty(doc.RootElement, "message").GetString());
    }

    private HttpClient CreateAnonymousClient() =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response)
    {
        var stream = await response.Content.ReadAsStreamAsync();
        return await JsonDocument.ParseAsync(stream);
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
