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

    [Fact]
    public async Task GetAuthMe_ConUsuarioSembrado_Retorna200ConShapeYRoles()
    {
        const string userId = "auth-me-user";
        using var auth = _factory.CreateClientWithTestAuth(userId: userId);

        await auth.SeedUserAsync(new ApplicationUser
        {
            Id = userId,
            UserName = "auth.me@test.local",
            Email = "auth.me@test.local",
            EmailConfirmed = true,
            Nombre = "Me",
            Apellido = "Test",
            Avatar = "avatar-3",
            Tema = "oscuro"
        }, "Empleado");

        var response = await auth.Client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = await ReadJsonAsync(response);
        var root = doc.RootElement;

        Assert.Equal(userId, GetProperty(root, "id").GetString());
        Assert.Equal("auth.me@test.local", GetProperty(root, "email").GetString());
        Assert.Equal("Me", GetProperty(root, "nombre").GetString());
        Assert.Equal("Test", GetProperty(root, "apellido").GetString());
        Assert.Equal("avatar-3", GetProperty(root, "avatar").GetString());
        Assert.Equal("oscuro", GetProperty(root, "tema").GetString());

        var roles = GetProperty(root, "roles");
        Assert.Equal(JsonValueKind.Array, roles.ValueKind);
        Assert.Contains("Empleado", roles.EnumerateArray().Select(role => role.GetString()).ToList());
    }

    [Fact]
    public async Task PostUsers_ConAdmin_DatosValidos_Retorna200ConIdYEmail()
    {
        const string email = "users.create@test.local";
        using var auth = _factory.CreateClientWithTestAuth(roles: ["Admin"]);
        await SeedRoleViaUserAsync(auth, "Empleado");

        var response = await auth.Client.PostAsJsonAsync("/api/users", new
        {
            nombre = "Nuevo",
            apellido = "Usuario",
            email,
            password = "Clave123!",
            rol = "Empleado"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = await ReadJsonAsync(response);
        var root = doc.RootElement;
        Assert.False(string.IsNullOrWhiteSpace(GetProperty(root, "id").GetString()));
        Assert.Equal(email, GetProperty(root, "email").GetString());
    }

    [Fact]
    public async Task PostUsers_EmailDuplicado_Retorna400ConMessage()
    {
        const string email = "users.duplicado@test.local";
        using var auth = _factory.CreateClientWithTestAuth(roles: ["Admin"]);
        await SeedRoleViaUserAsync(auth, "Empleado");

        var payload = new
        {
            nombre = "Primero",
            apellido = "Usuario",
            email,
            password = "Clave123!",
            rol = "Empleado"
        };
        var first = await auth.Client.PostAsJsonAsync("/api/users", payload);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        var duplicate = new
        {
            nombre = "Segundo",
            apellido = "Usuario",
            email,
            password = "Clave123!",
            rol = "Empleado"
        };
        var response = await auth.Client.PostAsJsonAsync("/api/users", duplicate);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var doc = await ReadJsonAsync(response);
        Assert.Equal("El email ya está registrado", GetProperty(doc.RootElement, "message").GetString());
    }

    [Fact]
    public async Task PostUsers_PayloadInvalido_Retorna400()
    {
        using var auth = _factory.CreateClientWithTestAuth(roles: ["Admin"]);

        var response = await auth.Client.PostAsJsonAsync("/api/users", new { });

        // No asertar el shape: puede venir de ModelState/ProblemDetails o de Identity.
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PutUsersRol_IdInexistente_Retorna404ConMessage()
    {
        using var auth = _factory.CreateClientWithTestAuth(roles: ["Admin"]);

        var response = await auth.Client.PutAsJsonAsync("/api/users/no-existe/rol", new { rol = "Admin" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        using var doc = await ReadJsonAsync(response);
        Assert.Equal("Usuario no encontrado.", GetProperty(doc.RootElement, "message").GetString());
    }

    [Fact]
    public async Task DeleteUsers_IdInexistente_Retorna404ConMessage()
    {
        using var auth = _factory.CreateClientWithTestAuth(roles: ["Admin"]);

        var response = await auth.Client.DeleteAsync("/api/users/no-existe");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        using var doc = await ReadJsonAsync(response);
        Assert.Equal("Usuario no encontrado.", GetProperty(doc.RootElement, "message").GetString());
    }

    [Fact]
    public async Task PutConfigProfile_Parcial_Retorna200SinSobrescribirVacios()
    {
        const string userId = "config-partial-user";
        using var auth = _factory.CreateClientWithTestAuth(userId: userId);

        await auth.SeedUserAsync(new ApplicationUser
        {
            Id = userId,
            UserName = "config.partial@test.local",
            Email = "config.partial@test.local",
            EmailConfirmed = true,
            Nombre = "Original",
            Apellido = "ApellidoOriginal",
            Avatar = "avatar-4",
            Tema = "oscuro",
            Paleta = "verde"
        }, "Empleado");

        // PerfilDto tiene defaults no vacíos: enviar explícitamente "" para las
        // propiedades que no deben sobrescribirse (el controller las ignora).
        var response = await auth.Client.PutAsJsonAsync("/api/config/profile", new
        {
            nombre = "Nuevo",
            apellido = "",
            avatar = "",
            tema = "",
            paleta = ""
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = await ReadJsonAsync(response);
        var root = doc.RootElement;

        Assert.Equal("Nuevo", GetProperty(root, "nombre").GetString());
        Assert.Equal("ApellidoOriginal", GetProperty(root, "apellido").GetString());
        Assert.Equal("avatar-4", GetProperty(root, "avatar").GetString());
        Assert.Equal("oscuro", GetProperty(root, "tema").GetString());
        Assert.Equal("verde", GetProperty(root, "paleta").GetString());

        var roles = GetProperty(root, "roles");
        Assert.Equal(JsonValueKind.Array, roles.ValueKind);
        Assert.Contains("Empleado", roles.EnumerateArray().Select(role => role.GetString()).ToList());
    }

    /// <summary>
    /// En producción los roles los crea DbSeeder; con Seed:RunOnStartup=false la
    /// DB de test no los tiene. Sembrar un usuario con el rol lo crea en RoleManager.
    /// </summary>
    private static async Task SeedRoleViaUserAsync(TestAuthClient auth, string role)
    {
        var suffix = Guid.NewGuid().ToString("N");
        await auth.SeedUserAsync(new ApplicationUser
        {
            Id = $"role-seed-{suffix}",
            UserName = $"role.seed.{suffix}@test.local",
            Email = $"role.seed.{suffix}@test.local",
            EmailConfirmed = true
        }, role);
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
