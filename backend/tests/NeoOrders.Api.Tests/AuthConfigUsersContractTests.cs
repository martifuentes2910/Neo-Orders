using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using NeoOrders.Infrastructure.Models;

namespace NeoOrders.Api.Tests;

/// <summary>
/// Smoke/contract tests de Auth, Usuarios y Configuración. Se apoyan en el
/// esquema de autenticación de prueba y en DB InMemory; no requieren login real.
/// </summary>
public class AuthConfigUsersContractTests : IClassFixture<NeoOrdersApiFactory>
{
    private readonly NeoOrdersApiFactory _factory;

    public AuthConfigUsersContractTests(NeoOrdersApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetAuthMe_SinAuth_Retorna401ConMessage()
    {
        using var client = CreateAnonymousClient();

        var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        using var doc = await ReadJsonAsync(response);
        Assert.Equal("No autenticado.", GetProperty(doc.RootElement, "message").GetString());
    }

    [Fact]
    public async Task GetUsers_SinRolAdmin_Retorna403()
    {
        using var auth = _factory.CreateClientWithTestAuth();

        var response = await auth.Client.GetAsync("/api/users");

        // El challenge del esquema de prueba no garantiza body: asertar solo status.
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetUsers_ConRolAdmin_Retorna200Lista()
    {
        using var auth = _factory.CreateClientWithTestAuth(roles: ["Admin"]);

        var response = await auth.Client.GetAsync("/api/users");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = await ReadJsonAsync(response);
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
    }

    [Fact]
    public async Task GetConfigProfile_ConUsuarioSembrado_Retorna200ConRoles()
    {
        const string userId = "config-profile-user";
        using var auth = _factory.CreateClientWithTestAuth(userId: userId);

        await auth.SeedUserAsync(new ApplicationUser
        {
            Id = userId,
            UserName = "config.profile@test.local",
            Email = "config.profile@test.local",
            EmailConfirmed = true,
            Nombre = "Ana",
            Apellido = "Prueba",
            Avatar = "avatar-2",
            Tema = "oscuro",
            Paleta = "verde"
        }, "Empleado");

        var response = await auth.Client.GetAsync("/api/config/profile");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = await ReadJsonAsync(response);
        var root = doc.RootElement;

        // Casing real del wire (camelCase); el helper tolera ambos casings.
        Assert.Equal(userId, GetProperty(root, "id").GetString());
        Assert.Equal("config.profile@test.local", GetProperty(root, "email").GetString());
        Assert.Equal("Ana", GetProperty(root, "nombre").GetString());
        Assert.Equal("Prueba", GetProperty(root, "apellido").GetString());
        Assert.Equal("avatar-2", GetProperty(root, "avatar").GetString());
        Assert.Equal("oscuro", GetProperty(root, "tema").GetString());
        Assert.Equal("verde", GetProperty(root, "paleta").GetString());

        var roles = GetProperty(root, "roles");
        Assert.Equal(JsonValueKind.Array, roles.ValueKind);
        Assert.Contains("Empleado", roles.EnumerateArray().Select(role => role.GetString()).ToList());
    }

    [Fact]
    public async Task PostAuthLogin_CredencialesInvalidas_Retorna401ConMessage()
    {
        using var client = CreateAnonymousClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { email = "no-existe@test.local", password = "Clave123!" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        using var doc = await ReadJsonAsync(response);
        Assert.Equal("Credenciales inválidas", GetProperty(doc.RootElement, "message").GetString());
    }

    private HttpClient CreateAnonymousClient() =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response)
    {
        var stream = await response.Content.ReadAsStreamAsync();
        return await JsonDocument.ParseAsync(stream);
    }

    /// <summary>
    /// Busca una propiedad tolerando camelCase/PascalCase para no acoplar el test
    /// a la política de naming del serializer.
    /// </summary>
    private static JsonElement GetProperty(JsonElement obj, string name)
    {
        if (obj.TryGetProperty(name, out var exact))
        {
            return exact;
        }

        foreach (var property in obj.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                return property.Value;
            }
        }

        throw new Xunit.Sdk.XunitException($"La respuesta JSON no contiene la propiedad '{name}'.");
    }
}
