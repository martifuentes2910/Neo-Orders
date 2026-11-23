using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NeoOrders.Infrastructure.Models;

namespace NeoOrders.Api.Tests;

/// <summary>
/// Contratos de alto valor para Clients, Sellers y Config no cubiertos por
/// <see cref="CatalogContractTests"/> ni <see cref="ConfigAdminContractTests"/>:
/// validación de ID en PUT, PUT/DELETE sobre IDs inexistentes, GET por ID
/// existente y perfil con usuario de claims no persistido en Identity.
/// Cada test usa su propio cliente autenticado y, por lo tanto, su propia base
/// InMemory aislada.
/// </summary>
public class ClientsSellersConfigContractTests : IClassFixture<NeoOrdersApiFactory>
{
    private readonly NeoOrdersApiFactory _factory;

    public ClientsSellersConfigContractTests(NeoOrdersApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PutClient_IdMismatch_Retorna400Message()
    {
        using var auth = _factory.CreateClientWithTestAuth(roles: ["Admin"]);
        var suffix = Guid.NewGuid().ToString("N");

        var response = await auth.Client.PutAsJsonAsync("/api/clients/1", ClientPayload(id: 2, suffix));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var doc = await ReadJsonAsync(response);
        Assert.Equal("El ID de la ruta no coincide con el cuerpo.", GetProperty(doc.RootElement, "message").GetString());
    }

    [Fact]
    public async Task PutClient_Inexistente_Retorna404Message()
    {
        using var auth = _factory.CreateClientWithTestAuth(roles: ["Admin"]);
        var suffix = Guid.NewGuid().ToString("N");
        var missingId = await MissingClientIdAsync(auth);

        var response = await auth.Client.PutAsJsonAsync($"/api/clients/{missingId}", ClientPayload(missingId, suffix));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        using var doc = await ReadJsonAsync(response);
        Assert.Equal("Cliente no encontrado.", GetProperty(doc.RootElement, "message").GetString());
    }

    [Fact]
    public async Task PutClient_SinRolAdmin_Retorna403()
    {
        using var auth = _factory.CreateClientWithTestAuth();
        var suffix = Guid.NewGuid().ToString("N");

        var response = await auth.Client.PutAsJsonAsync("/api/clients/1", ClientPayload(id: 1, suffix));

        // El challenge del esquema de prueba no garantiza body: asertar solo status.
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task PutSeller_IdMismatch_Retorna400Message()
    {
        using var auth = _factory.CreateClientWithTestAuth(roles: ["Admin"]);
        var suffix = Guid.NewGuid().ToString("N");

        var response = await auth.Client.PutAsJsonAsync("/api/sellers/1", SellerPayload(id: 2, suffix));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var doc = await ReadJsonAsync(response);
        Assert.Equal("El ID de la ruta no coincide con el cuerpo.", GetProperty(doc.RootElement, "message").GetString());
    }

    [Fact]
    public async Task PutSeller_Inexistente_Retorna404Message()
    {
        using var auth = _factory.CreateClientWithTestAuth(roles: ["Admin"]);
        var suffix = Guid.NewGuid().ToString("N");
        var missingId = await MissingSellerIdAsync(auth);

        var response = await auth.Client.PutAsJsonAsync($"/api/sellers/{missingId}", SellerPayload(missingId, suffix));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        using var doc = await ReadJsonAsync(response);
        Assert.Equal("Vendedor no encontrado.", GetProperty(doc.RootElement, "message").GetString());
    }

    [Fact]
    public async Task GetSellerById_Existente_Retorna200()
    {
        using var auth = _factory.CreateClientWithTestAuth();
        var suffix = Guid.NewGuid().ToString("N");
        var name = $"Vendedor contrato {suffix}";
        var email = $"seller.contract.{suffix}@test.local";

        var sellerId = await auth.QueryDbAsync(async db =>
        {
            var seller = new Seller
            {
                Name = name,
                Email = email,
                Phone = "987654321",
                CommissionPercentage = 7.5m,
                Active = true
            };
            db.Sellers.Add(seller);
            await db.SaveChangesAsync();
            return seller.Id;
        });

        var response = await auth.Client.GetAsync($"/api/sellers/{sellerId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = await ReadJsonAsync(response);
        var root = doc.RootElement;
        Assert.Equal(sellerId, GetProperty(root, "id").GetInt32());
        Assert.Equal(name, GetProperty(root, "name").GetString());
        Assert.Equal(email, GetProperty(root, "email").GetString());
    }

    [Fact]
    public async Task GetConfigProfile_SinUsuario_Retorna401Message()
    {
        // El claim NameIdentifier apunta a un usuario que no se siembra en Identity.
        using var auth = _factory.CreateClientWithTestAuth(userId: $"missing-profile-user-{Guid.NewGuid():N}");

        var response = await auth.Client.GetAsync("/api/config/profile");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        using var doc = await ReadJsonAsync(response);
        Assert.Equal("No autenticado.", GetProperty(doc.RootElement, "message").GetString());
    }

    [Fact]
    public async Task PutConfigProfile_SinUsuario_Retorna401Message()
    {
        using var auth = _factory.CreateClientWithTestAuth(userId: $"missing-profile-user-{Guid.NewGuid():N}");
        var payload = new
        {
            nombre = "Sin",
            apellido = "Usuario",
            avatar = "avatar-1",
            tema = "claro",
            paleta = "coral"
        };

        var response = await auth.Client.PutAsJsonAsync("/api/config/profile", payload);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        using var doc = await ReadJsonAsync(response);
        Assert.Equal("No autenticado.", GetProperty(doc.RootElement, "message").GetString());
    }

    private static object ClientPayload(int id, string suffix) => new
    {
        id,
        nombre = $"Cliente contrato {suffix}",
        email = $"cliente.contrato.{suffix}@test.local",
        telefono = "123456789",
        direccion = "Calle Contrato 1"
    };

    private static object SellerPayload(int id, string suffix) => new
    {
        id,
        name = $"Vendedor contrato {suffix}",
        email = $"vendedor.contrato.{suffix}@test.local",
        phone = "987654321",
        commissionPercentage = 5m,
        active = true
    };

    private static async Task<int> MissingClientIdAsync(TestAuthClient auth) =>
        await auth.QueryDbAsync(async db =>
            (await db.Clients.Select(c => c.Id).ToListAsync()).DefaultIfEmpty(0).Max() + 1);

    private static async Task<int> MissingSellerIdAsync(TestAuthClient auth) =>
        await auth.QueryDbAsync(async db =>
            (await db.Sellers.Select(s => s.Id).ToListAsync()).DefaultIfEmpty(0).Max() + 1);

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
