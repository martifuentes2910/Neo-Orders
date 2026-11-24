using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace NeoOrders.Api.Tests;

public class ConfigAdminContractTests : IClassFixture<NeoOrdersApiFactory>
{
    private readonly NeoOrdersApiFactory _factory;

    public ConfigAdminContractTests(NeoOrdersApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PutConfigCompany_SinRolAdmin_Retorna403()
    {
        using var auth = _factory.CreateClientWithTestAuth();
        var response = await auth.Client.PutAsJsonAsync("/api/config/company", new { nombreEmpresa = "X", moneda = "ARS", tipoDeCambio = 1m });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task PutConfigCompany_ConRolAdmin_Retorna200()
    {
        using var auth = _factory.CreateClientWithTestAuth(roles: ["Admin"]);
        var response = await auth.Client.PutAsJsonAsync("/api/config/company", new { nombreEmpresa = "NeoOrders Test", moneda = "ARS", tipoDeCambio = 1200m });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = await ReadJsonAsync(response);
        Assert.Equal("NeoOrders Test", GetProperty(doc.RootElement, "nombreEmpresa").GetString());
    }

    [Fact]
    public async Task PutConfigCompany_CuerpoJsonInvalido_Retorna400()
    {
        using var auth = _factory.CreateClientWithTestAuth(roles: ["Admin"]);

        using var body = new StringContent("{", Encoding.UTF8, "application/json");
        var response = await auth.Client.PutAsync("/api/config/company", body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var mediaType = response.Content.Headers.ContentType?.MediaType ?? string.Empty;
        if (mediaType.Contains("json", StringComparison.OrdinalIgnoreCase))
        {
            using var doc = await ReadJsonAsync(response);
            Assert.True(
                doc.RootElement.TryGetProperty("errors", out _) ||
                doc.RootElement.TryGetProperty("message", out _));
        }
    }

    [Fact]
    public async Task PutConfigProfile_CuerpoJsonInvalido_Retorna400ConShape()
    {
        using var auth = _factory.CreateClientWithTestAuth();

        // [ApiController] rechaza el cuerpo no enlazable antes de ejecutar la
        // acción: no hace falta que el usuario exista en Identity.
        using var body = new StringContent("{", Encoding.UTF8, "application/json");
        var response = await auth.Client.PutAsync("/api/config/profile", body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var mediaType = response.Content.Headers.ContentType?.MediaType ?? string.Empty;
        Assert.Contains("json", mediaType, StringComparison.OrdinalIgnoreCase);
        using var doc = await ReadJsonAsync(response);
        Assert.True(
            doc.RootElement.TryGetProperty("errors", out _) ||
            doc.RootElement.TryGetProperty("message", out _));
    }

    [Fact]
    public async Task PutConfigCompany_PayloadVacio_Retorna200ConValoresPorDefecto()
    {
        using var auth = _factory.CreateClientWithTestAuth(roles: ["Admin"]);

        // CompanySettingsDto no tiene DataAnnotations: el payload vacío se acepta
        // y se persiste con los valores iniciales del DTO.
        var response = await auth.Client.PutAsJsonAsync("/api/config/company", new { });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = await ReadJsonAsync(response);
        var root = doc.RootElement;
        Assert.Equal("NeoOrders", GetProperty(root, "nombreEmpresa").GetString());
        Assert.Equal("ARS", GetProperty(root, "moneda").GetString());
        Assert.Equal(1200m, GetProperty(root, "tipoDeCambio").GetDecimal());
    }

    [Fact]
    public async Task PostConfigSeed_ConRolAdmin_Retorna200ConMessage()
    {
        using var auth = _factory.CreateClientWithTestAuth(roles: ["Admin"]);
        var response = await auth.Client.PostAsync("/api/config/seed", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = await ReadJsonAsync(response);
        Assert.Equal("Datos de ejemplo verificados/cargados", GetProperty(doc.RootElement, "message").GetString());
    }

    [Fact]
    public async Task PostConfigSeed_SinRolAdmin_Retorna403()
    {
        using var auth = _factory.CreateClientWithTestAuth();
        var response = await auth.Client.PostAsync("/api/config/seed", null);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task PostConfigClear_ConRolAdmin_Retorna200ConShape()
    {
        using var auth = _factory.CreateClientWithTestAuth(roles: ["Admin"]);
        var response = await auth.Client.PostAsync("/api/config/clear", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = await ReadJsonAsync(response);
        var root = doc.RootElement;
        _ = GetProperty(root, "message");
        var eliminados = GetProperty(root, "eliminados");
        Assert.Equal(JsonValueKind.Object, eliminados.ValueKind);
    }

    [Fact]
    public async Task PostConfigClear_SinRolAdmin_Retorna403()
    {
        using var auth = _factory.CreateClientWithTestAuth();
        var response = await auth.Client.PostAsync("/api/config/clear", null);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task PostAuthLogout_SinAuth_Retorna204()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var response = await client.PostAsync("/api/auth/logout", null);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

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
