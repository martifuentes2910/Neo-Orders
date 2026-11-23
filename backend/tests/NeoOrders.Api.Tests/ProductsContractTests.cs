using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NeoOrders.Infrastructure.Models;

namespace NeoOrders.Api.Tests;

/// <summary>
/// Contratos HTTP de /api/products no cubiertos por CatalogContractTests ni
/// CatalogGapsContractTests: validación de ID en PUT, faltantes, imagen y
/// autorización de ajustes de precio. Cada cliente autenticado usa una factory
/// derivada con su propia base InMemory aislada.
/// </summary>
public class ProductsContractTests : IClassFixture<NeoOrdersApiFactory>
{
    private readonly NeoOrdersApiFactory _factory;

    public ProductsContractTests(NeoOrdersApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PutProduct_IdMismatch_Retorna400Message()
    {
        using var auth = _factory.CreateClientWithTestAuth(roles: ["Admin"]);

        var response = await auth.Client.PutAsJsonAsync("/api/products/1", CreateProductDto(id: 2));

        await AssertBadRequestMessageAsync(response, "El ID de la ruta no coincide con el cuerpo.");
    }

    [Fact]
    public async Task PutProduct_Inexistente_Retorna404Message()
    {
        using var auth = _factory.CreateClientWithTestAuth(roles: ["Admin"]);
        var missingId = await GetMissingProductIdAsync(auth);

        var response = await auth.Client.PutAsJsonAsync($"/api/products/{missingId}", CreateProductDto(missingId));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        using var doc = await ReadJsonAsync(response);
        Assert.Equal("Producto no encontrado.", GetProperty(doc.RootElement, "message").GetString());
    }

    [Fact]
    public async Task DeleteProductImage_Inexistente_Retorna404Message()
    {
        using var auth = _factory.CreateClientWithTestAuth(roles: ["Admin"]);
        var missingId = await GetMissingProductIdAsync(auth);

        var response = await auth.Client.DeleteAsync($"/api/products/{missingId}/image");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        using var doc = await ReadJsonAsync(response);
        Assert.Equal("Producto no encontrado.", GetProperty(doc.RootElement, "message").GetString());
    }

    /// <summary>
    /// El controller declara `if (file == null) return BadRequest(new { message = ... })`,
    /// pero con Nullable enable + [ApiController] el IFormFile no anulable recibe un
    /// [Required] implícito: el filtro de ModelState devuelve 400 antes de la acción.
    /// El contrato observable es ValidationProblemDetails con `errors.file`.
    /// </summary>
    [Fact]
    public async Task PostProductImage_SinArchivo_Retorna400Message()
    {
        using var auth = _factory.CreateClientWithTestAuth(roles: ["Admin"]);
        var productId = await SeedProductAsync(auth);
        using var form = CreateFormWithoutFile();

        var response = await auth.Client.PostAsync($"/api/products/{productId}/image", form);

        await AssertBadRequestFileRequiredAsync(response);
    }

    /// <summary>
    /// Smoke del endpoint de subida con un archivo válido. En Testing no hay
    /// credenciales de Cloudinary, así que el contrato observable es 400 con el
    /// mensaje de "no configurado" (guard de ProductService.UploadImageAsync).
    /// Cubre el binding multipart y el dispatch al servicio, no cubiertos por
    /// PostProductImage_SinArchivo.
    /// </summary>
    [Fact]
    public async Task PostProductImage_ArchivoValidoSinCloudinary_Retorna400ConMessage()
    {
        using var auth = _factory.CreateClientWithTestAuth(roles: ["Admin"]);
        var productId = await SeedProductAsync(auth);
        using var form = new MultipartFormDataContent();
        var png = new ByteArrayContent(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });
        png.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        form.Add(png, "file", "producto.png");

        var response = await auth.Client.PostAsync($"/api/products/{productId}/image", form);

        await AssertBadRequestMessageAsync(response,
            "Almacenamiento de imágenes no configurado. Definí Cloudinary__CloudName, Cloudinary__ApiKey y Cloudinary__ApiSecret.");
    }

    /// <summary>
    /// Happy path de DELETE /api/products/{id}/image: con storage no configurado,
    /// RemoveImageAsync igual limpia ImageUrl/ImagePublicId y devuelve 204.
    /// </summary>
    [Fact]
    public async Task DeleteProductImage_Existente_Retorna204YLimpiaImagen()
    {
        using var auth = _factory.CreateClientWithTestAuth(roles: ["Admin"]);
        var productId = await SeedProductAsync(auth);
        await auth.QueryDbAsync(async db =>
        {
            var product = (await db.Products.FindAsync(productId))!;
            product.ImageUrl = "https://res.cloudinary.com/test/image/upload/sample.png";
            product.ImagePublicId = "neoorders/products/sample";
            await db.SaveChangesAsync();
            return true;
        });

        var response = await auth.Client.DeleteAsync($"/api/products/{productId}/image");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var image = await auth.QueryDbAsync(async db =>
        {
            var product = (await db.Products.FindAsync(productId))!;
            return (product.ImageUrl, product.ImagePublicId);
        });
        Assert.Null(image.ImageUrl);
        Assert.Null(image.ImagePublicId);
    }

    [Fact]
    public async Task PreviewPriceAdjustment_SinAdmin_Retorna403()
    {
        using var auth = _factory.CreateClientWithTestAuth();

        var response = await auth.Client.PostAsJsonAsync("/api/products/price-adjustment/preview", new { percent = 10m });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ApplyPriceAdjustment_SinAdmin_Retorna403()
    {
        using var auth = _factory.CreateClientWithTestAuth();

        var response = await auth.Client.PostAsJsonAsync("/api/products/price-adjustment/apply", new { percent = 10m });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task PutProduct_Valido_Retorna204()
    {
        using var auth = _factory.CreateClientWithTestAuth(roles: ["Admin"]);
        var productId = await SeedProductAsync(auth);
        var suffix = Guid.NewGuid().ToString("N");

        var response = await auth.Client.PutAsJsonAsync($"/api/products/{productId}", ValidProductPayload(productId, suffix));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var persisted = await auth.QueryDbAsync(async db =>
        {
            var product = await db.Products.FindAsync(productId);
            return (Sku: product!.Sku, Name: product.Name, Stock: product.Stock);
        });
        Assert.Equal($"SKU-PRODUCT-UPDATE-{suffix}", persisted.Sku);
        Assert.Equal($"Producto actualizado {suffix}", persisted.Name);
        Assert.Equal(7, persisted.Stock);
    }

    [Fact]
    public async Task DeleteProduct_Existente_Admin_Retorna204()
    {
        using var auth = _factory.CreateClientWithTestAuth(roles: ["Admin"]);
        var productId = await SeedProductAsync(auth);

        var response = await auth.Client.DeleteAsync($"/api/products/{productId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.False(await auth.QueryDbAsync(async db => await db.Products.AnyAsync(p => p.Id == productId)));
    }

    [Fact]
    public async Task GetProductById_Existente_Retorna200()
    {
        using var auth = _factory.CreateClientWithTestAuth();
        var productId = await SeedProductAsync(auth);
        var sku = await auth.QueryDbAsync(async db => (await db.Products.FindAsync(productId))!.Sku);

        var response = await auth.Client.GetAsync($"/api/products/{productId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = await ReadJsonAsync(response);
        Assert.Equal(productId, GetProperty(doc.RootElement, "id").GetInt32());
        Assert.Equal(sku, GetProperty(doc.RootElement, "sku").GetString());
    }

    [Fact]
    public async Task GetProducts_ConAuth_Retorna200Array()
    {
        using var auth = _factory.CreateClientWithTestAuth();
        var productId = await SeedProductAsync(auth);

        var response = await auth.Client.GetAsync("/api/products");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = await ReadJsonAsync(response);
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
        Assert.Contains(doc.RootElement.EnumerateArray(), p => GetProperty(p, "id").GetInt32() == productId);
    }

    private static object ValidProductPayload(int id, string suffix) => new
    {
        id,
        sku = $"SKU-PRODUCT-UPDATE-{suffix}",
        name = $"Producto actualizado {suffix}",
        category = "General",
        basePrice = 12.50m,
        cost = 6.25m,
        stock = 7,
        minStockAlert = 2,
        active = true
    };

    private static object CreateProductDto(int id) => new
    {
        id,
        sku = $"SKU-PRODUCT-CONTRACT-{id}",
        name = "Producto contrato",
        category = "General",
        basePrice = 10.50m,
        cost = 5.25m,
        stock = 5,
        minStockAlert = 1,
        active = true
    };

    private static async Task<int> SeedProductAsync(TestAuthClient auth)
    {
        var suffix = Guid.NewGuid().ToString("N");
        return await auth.QueryDbAsync(async db =>
        {
            var product = new Product
            {
                Sku = $"SKU-PRODUCT-IMG-{suffix}",
                Name = $"Producto imagen {suffix}",
                Category = "General",
                BasePrice = 10.50m,
                Cost = 5.25m,
                Stock = 5,
                MinStockAlert = 1,
                Active = true
            };
            db.Products.Add(product);
            await db.SaveChangesAsync();
            return product.Id;
        });
    }

    private static Task<int> GetMissingProductIdAsync(TestAuthClient auth) =>
        auth.QueryDbAsync(async db => (await db.Products.Select(p => p.Id).ToListAsync()).DefaultIfEmpty(0).Max() + 1);

    /// <summary>
    /// Multipart válido sin el campo "file" (un form vacío se serializa como
    /// sección inválida), igual que en CsvImportContractTests.
    /// </summary>
    private static MultipartFormDataContent CreateFormWithoutFile()
    {
        var form = new MultipartFormDataContent();
        form.Add(new StringContent("ignored"), "otroCampo");
        return form;
    }

    private static async Task AssertBadRequestMessageAsync(HttpResponseMessage response, string expectedMessage)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var doc = await ReadJsonAsync(response);
        Assert.True(doc.RootElement.TryGetProperty("message", out var message),
            $"La respuesta 400 no contiene 'message': {doc.RootElement.GetRawText()}");
        Assert.Equal(expectedMessage, message.GetString());
    }

    private static async Task AssertBadRequestFileRequiredAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var doc = await ReadJsonAsync(response);
        Assert.True(doc.RootElement.TryGetProperty("errors", out var errors),
            $"La respuesta 400 no contiene 'errors': {doc.RootElement.GetRawText()}");
        Assert.True(errors.TryGetProperty("file", out var fileErrors),
            $"Los errores no contienen 'file': {errors.GetRawText()}");
        Assert.Contains(fileErrors.EnumerateArray(), e =>
            e.GetString()?.Contains("file", StringComparison.OrdinalIgnoreCase) == true);
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
