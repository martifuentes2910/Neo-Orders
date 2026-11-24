using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace NeoOrders.Api.Tests;

public class OrdersDashboardContractTests : IClassFixture<NeoOrdersApiFactory>
{
    private readonly NeoOrdersApiFactory _factory;

    public OrdersDashboardContractTests(NeoOrdersApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetOrders_SinAuth_Retorna401()
    {
        using var client = CreateAnonymousClient();
        var response = await client.GetAsync("/api/orders");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetOrderById_Inexistente_Retorna404ConMessage()
    {
        using var auth = _factory.CreateClientWithTestAuth();
        var response = await auth.Client.GetAsync("/api/orders/999999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        using var doc = await ReadJsonAsync(response);
        Assert.Equal("Pedido no encontrado.", GetProperty(doc.RootElement, "message").GetString());
    }

    [Fact]
    public async Task DeleteOrder_Inexistente_Admin_Retorna404ConMessage()
    {
        using var auth = _factory.CreateClientWithTestAuth(roles: ["Admin"]);
        var response = await auth.Client.DeleteAsync("/api/orders/999999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        using var doc = await ReadJsonAsync(response);
        Assert.Equal("Pedido no encontrado.", GetProperty(doc.RootElement, "message").GetString());
    }

    [Fact]
    public async Task DeleteOrder_SinRolAdmin_Retorna403()
    {
        using var auth = _factory.CreateClientWithTestAuth();
        var response = await auth.Client.DeleteAsync("/api/orders/1");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task PostOrders_ModelStateInvalido_Retorna400()
    {
        using var auth = _factory.CreateClientWithTestAuth();
        var response = await auth.Client.PostAsJsonAsync("/api/orders", new { cantidad = 0 });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PatchOrderStatus_Inexistente_Retorna404ConMessage()
    {
        using var auth = _factory.CreateClientWithTestAuth();
        var response = await auth.Client.PatchAsJsonAsync("/api/orders/999999/estado", new { estado = "Procesado" });
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        using var doc = await ReadJsonAsync(response);
        Assert.False(string.IsNullOrWhiteSpace(GetProperty(doc.RootElement, "message").GetString()));
    }

    [Fact]
    public async Task GetOrderStatuses_SinAuth_Retorna401()
    {
        using var client = CreateAnonymousClient();
        var response = await client.GetAsync("/api/orders/statuses");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetDashboardMetrics_SinAuth_Retorna401()
    {
        using var client = CreateAnonymousClient();
        var response = await client.GetAsync("/api/dashboard/metrics");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetDashboardMetrics_ConAuth_Retorna200()
    {
        using var auth = _factory.CreateClientWithTestAuth();
        var response = await auth.Client.GetAsync("/api/dashboard/metrics");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = await ReadJsonAsync(response);
        Assert.Equal(JsonValueKind.Object, doc.RootElement.ValueKind);
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
