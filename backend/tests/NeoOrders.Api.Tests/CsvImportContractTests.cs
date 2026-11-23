using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace NeoOrders.Api.Tests;

/// <summary>
/// Contratos HTTP de las plantillas y la importación CSV. Todas las peticiones
/// usan autenticación de prueba con rol Admin y una base InMemory aislada.
/// </summary>
public class CsvImportContractTests : IClassFixture<NeoOrdersApiFactory>
{
    private const string MensajeExtensionInvalida = "El archivo debe tener extensión .csv.";

    private readonly NeoOrdersApiFactory _factory;

    public CsvImportContractTests(NeoOrdersApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetProductsImportTemplate_ConAdmin_Retorna200Csv()
    {
        using var auth = _factory.CreateClientWithTestAuth(roles: ["Admin"]);

        var response = await auth.Client.GetAsync("/api/products/import-csv/template");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("text/csv", response.Content.Headers.ContentType?.ToString(), StringComparison.OrdinalIgnoreCase);
        var body = await response.Content.ReadAsStringAsync();
        Assert.StartsWith("Sku;Name;Category;BasePrice;Cost;Stock;MinStockAlert;Active", body, StringComparison.Ordinal);
    }

    // Los controllers declaran `if (file == null) return BadRequest(new { message = ... })`,
    // pero con Nullable enable + [ApiController] el parámetro IFormFile no anulable
    // recibe un [Required] implícito: el filtro de ModelState devuelve 400 antes de
    // entrar a la acción. El contrato observable es ValidationProblemDetails.
    [Fact]
    public async Task PostProductsImportCsv_SinArchivo_Retorna400ConErrorDeValidacion()
    {
        using var auth = _factory.CreateClientWithTestAuth(roles: ["Admin"]);
        using var form = CreateFormWithoutFile();

        var response = await auth.Client.PostAsync("/api/products/import-csv", form);

        await AssertBadRequestFileRequiredAsync(response);
    }

    [Fact]
    public async Task PostProductsImportCsv_ExtensionInvalida_Retorna400Message()
    {
        using var auth = _factory.CreateClientWithTestAuth(roles: ["Admin"]);
        using var form = CreateForm("sku;name\nX1;Prod\n", "datos.txt");

        var response = await auth.Client.PostAsync("/api/products/import-csv", form);

        await AssertBadRequestMessageAsync(response, MensajeExtensionInvalida);
    }

    [Fact]
    public async Task PostClientsImportCsv_SinArchivo_Retorna400ConErrorDeValidacion()
    {
        using var auth = _factory.CreateClientWithTestAuth(roles: ["Admin"]);
        using var form = CreateFormWithoutFile();

        var response = await auth.Client.PostAsync("/api/clients/import-csv", form);

        await AssertBadRequestFileRequiredAsync(response);
    }

    [Fact]
    public async Task PostOrdersImportCsv_SinArchivo_Retorna400ConErrorDeValidacion()
    {
        using var auth = _factory.CreateClientWithTestAuth(roles: ["Admin"]);
        using var form = CreateFormWithoutFile();

        var response = await auth.Client.PostAsync("/api/orders/import-csv", form);

        await AssertBadRequestFileRequiredAsync(response);
    }

    [Fact]
    public async Task PostOrdersImportCsv_ExtensionInvalida_Retorna400Message()
    {
        using var auth = _factory.CreateClientWithTestAuth(roles: ["Admin"]);
        using var form = CreateForm("OrderNumber;ClientId\nO1;1\n", "pedidos.txt");

        var response = await auth.Client.PostAsync("/api/orders/import-csv", form);

        await AssertBadRequestMessageAsync(response, MensajeExtensionInvalida);
    }

    /// <summary>
    /// Formulario multipart válido que no contiene el campo "file": un form
    /// vacío es serializado por HttpClient como una sección inválida.
    /// </summary>
    private static MultipartFormDataContent CreateFormWithoutFile()
    {
        var form = new MultipartFormDataContent();
        form.Add(new StringContent("ignored"), "otroCampo");
        return form;
    }

    private static MultipartFormDataContent CreateForm(string content, string fileName)
    {
        var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(Encoding.UTF8.GetBytes(content));
        file.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        form.Add(file, "file", fileName);
        return form;
    }

    private static async Task AssertBadRequestMessageAsync(HttpResponseMessage response, string expectedMessage)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        Assert.True(doc.RootElement.TryGetProperty("message", out var message),
            $"La respuesta 400 no contiene 'message': {doc.RootElement.GetRawText()}");
        Assert.Equal(expectedMessage, message.GetString());
    }

    private static async Task AssertBadRequestFileRequiredAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        Assert.True(doc.RootElement.TryGetProperty("errors", out var errors),
            $"La respuesta 400 no contiene 'errors': {doc.RootElement.GetRawText()}");
        Assert.True(errors.TryGetProperty("file", out var fileErrors),
            $"Los errores no contienen 'file': {errors.GetRawText()}");
        Assert.Contains(fileErrors.EnumerateArray(), e =>
            e.GetString()?.Contains("file", StringComparison.OrdinalIgnoreCase) == true);
    }
}
