using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace NeoOrders.Api.Tests;

/// <summary>
/// Contratos de validación de entrada del catálogo (KAN-41/KAN-42). Los DTOs de
/// Application no tienen DataAnnotations: un payload con campos omitidos se
/// acepta, mientras que un cuerpo JSON no enlazable es rechazado por
/// [ApiController] con 400. Cada petición usa una factory derivada con su propia
/// DB InMemory aislada.
/// </summary>
public class CatalogValidationContractTests : IClassFixture<NeoOrdersApiFactory>
{
    private readonly NeoOrdersApiFactory _factory;

    public CatalogValidationContractTests(NeoOrdersApiFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("/api/products")]
    [InlineData("/api/clients")]
    [InlineData("/api/sellers")]
    public async Task PostCatalogo_CuerpoJsonInvalido_Retorna400(string url)
    {
        using var auth = _factory.CreateClientWithTestAuth(roles: ["Admin"]);
        using var body = new StringContent("{", Encoding.UTF8, "application/json");

        var response = await auth.Client.PostAsync(url, body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertBadRequestShapeAsync(response);
    }

    [Theory]
    [InlineData("/api/products/1")]
    [InlineData("/api/clients/1")]
    [InlineData("/api/sellers/1")]
    public async Task PutCatalogo_CuerpoJsonInvalido_Retorna400(string url)
    {
        using var auth = _factory.CreateClientWithTestAuth(roles: ["Admin"]);
        using var body = new StringContent("{", Encoding.UTF8, "application/json");

        var response = await auth.Client.PutAsync(url, body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertBadRequestShapeAsync(response);
    }

    [Theory]
    [InlineData("/api/products")]
    [InlineData("/api/clients")]
    [InlineData("/api/sellers")]
    public async Task PostCatalogo_PayloadVacio_Retorna201ComportamientoReal(string url)
    {
        using var auth = _factory.CreateClientWithTestAuth(roles: ["Admin"]);

        var response = await auth.Client.PostAsJsonAsync(url, new { });

        // No hay atributos de validación en ProductDto/ClientDto/SellerDto: los
        // campos ausentes toman sus valores por defecto y la creación prospera.
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private static async Task AssertBadRequestShapeAsync(HttpResponseMessage response)
    {
        var mediaType = response.Content.Headers.ContentType?.MediaType ?? string.Empty;
        Assert.Contains("json", mediaType, StringComparison.OrdinalIgnoreCase);
        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        Assert.Equal(JsonValueKind.Object, doc.RootElement.ValueKind);
        Assert.True(
            doc.RootElement.TryGetProperty("errors", out _) ||
            doc.RootElement.TryGetProperty("message", out _),
            $"La respuesta 400 no expone 'errors' ni 'message': {doc.RootElement.GetRawText()}");
    }
}
