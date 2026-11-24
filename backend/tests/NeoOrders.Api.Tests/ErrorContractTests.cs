using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NeoOrders.Application.Contracts.Clients;
using NeoOrders.Application.Contracts.Sellers;
using NeoOrders.Application.DTOs.Clients;
using NeoOrders.Application.DTOs.Sellers;

namespace NeoOrders.Api.Tests;

/// <summary>
/// Contratos de error (auditoría KAN-39): los InvalidOperationException mapeados
/// por los controllers siguen siendo 400 {message}, y cualquier excepción no
/// manejada la absorbe GlobalExceptionHandler con 500 {message, traceId}.
/// Los stubs simulan violaciones de UNIQUE y fallos inesperados porque el
/// provider InMemory no aplica índices únicos. No se modifica código productivo.
/// </summary>
public class ErrorContractTests : IClassFixture<NeoOrdersApiFactory>
{
    private readonly NeoOrdersApiFactory _factory;

    public ErrorContractTests(NeoOrdersApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PostClient_EmailDuplicado_Retorna400ConMessage()
    {
        using var auth = _factory.CreateClientWithTestAuth(
            roles: ["Admin"],
            configureServices: services =>
            {
                services.RemoveAll<IClientService>();
                services.AddScoped<IClientService, DuplicateEmailClientService>();
            });

        var response = await auth.Client.PostAsJsonAsync("/api/clients", new
        {
            nombre = "Cliente duplicado",
            email = "cliente.duplicado@test.local"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var doc = await ReadJsonAsync(response);
        Assert.Equal("Ya existe un cliente con ese email.", GetProperty(doc.RootElement, "message").GetString());
    }

    [Fact]
    public async Task PostSeller_EmailDuplicado_Retorna400ConMessage()
    {
        using var auth = _factory.CreateClientWithTestAuth(
            roles: ["Admin"],
            configureServices: services =>
            {
                services.RemoveAll<ISellerService>();
                services.AddScoped<ISellerService, DuplicateEmailSellerService>();
            });

        var response = await auth.Client.PostAsJsonAsync("/api/sellers", new
        {
            name = "Vendedor duplicado",
            email = "vendedor.duplicado@test.local"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var doc = await ReadJsonAsync(response);
        Assert.Equal("Ya existe un vendedor con ese email.", GetProperty(doc.RootElement, "message").GetString());
    }

    [Fact]
    public async Task GetClients_ExcepcionNoManejada_Retorna500ConMessageYTraceId()
    {
        using var auth = _factory.CreateClientWithTestAuth(
            configureServices: services =>
            {
                services.RemoveAll<IClientService>();
                services.AddScoped<IClientService, ThrowingClientService>();
            });

        var response = await auth.Client.GetAsync("/api/clients");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        using var doc = await ReadJsonAsync(response);
        var root = doc.RootElement;
        Assert.False(string.IsNullOrWhiteSpace(GetProperty(root, "message").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(GetProperty(root, "traceId").GetString()));
        // Testing no es Development: el detalle técnico ('code') no se expone.
        Assert.False(root.TryGetProperty("code", out _));
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

    private sealed class DuplicateEmailClientService : IClientService
    {
        public Task<List<ClientListItemDto>> ListAsync(string? search) =>
            Task.FromResult(new List<ClientListItemDto>());
        public Task<ClientDto?> GetByIdAsync(int id) => Task.FromResult<ClientDto?>(null);
        public Task<ClientDto> CreateAsync(ClientDto dto) =>
            throw new InvalidOperationException("Ya existe un cliente con ese email.");
        public Task<bool> UpdateAsync(int id, ClientDto dto) => Task.FromResult(false);
        public Task<(bool Ok, string? Error)> DeleteAsync(int id) => Task.FromResult((false, (string?)null));
    }

    private sealed class DuplicateEmailSellerService : ISellerService
    {
        public Task<List<SellerDto>> ListAsync(bool? onlyActive) => Task.FromResult(new List<SellerDto>());
        public Task<SellerDto?> GetByIdAsync(int id) => Task.FromResult<SellerDto?>(null);
        public Task<SellerDto> CreateAsync(SellerDto dto) =>
            throw new InvalidOperationException("Ya existe un vendedor con ese email.");
        public Task<bool> UpdateAsync(int id, SellerDto dto) => Task.FromResult(false);
        public Task<(bool Ok, string? Error)> DeleteAsync(int id) => Task.FromResult((false, (string?)null));
    }

    private sealed class ThrowingClientService : IClientService
    {
        public Task<List<ClientListItemDto>> ListAsync(string? search) =>
            throw new InvalidOperationException("Fallo inesperado de prueba.");
        public Task<ClientDto?> GetByIdAsync(int id) => Task.FromResult<ClientDto?>(null);
        public Task<ClientDto> CreateAsync(ClientDto dto) => Task.FromResult(dto);
        public Task<bool> UpdateAsync(int id, ClientDto dto) => Task.FromResult(false);
        public Task<(bool Ok, string? Error)> DeleteAsync(int id) => Task.FromResult((false, (string?)null));
    }
}
