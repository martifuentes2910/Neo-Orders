using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace NeoOrders.Api.Tests;

/// <summary>
/// Contratos HTTP de las plantillas y la importación CSV. Todas las peticiones
/// usan autenticación de prueba con rol Admin y una base InMemory aislada.
/// </summary>
public class CsvImportContractTests : IClassFixture<NeoOrdersApiFactory>
{
    private const string MensajeArchivoRequerido = "Debe seleccionar un archivo CSV.";
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

    // Contrato file-required: el parámetro IFormFile? evita el [Required] implícito
    // de [ApiController], de modo que se ejecuta el guard de la acción y la respuesta
    // es 400 { message }, el mismo shape que el resto de errores de importación.
    [Fact]
    public async Task PostProductsImportCsv_SinArchivo_Retorna400ConMessage()
    {
        using var auth = _factory.CreateClientWithTestAuth(roles: ["Admin"]);
        using var form = CreateFormWithoutFile();

        var response = await auth.Client.PostAsync("/api/products/import-csv", form);

        await AssertBadRequestMessageAsync(response, MensajeArchivoRequerido);
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
    public async Task PostClientsImportCsv_SinArchivo_Retorna400ConMessage()
    {
        using var auth = _factory.CreateClientWithTestAuth(roles: ["Admin"]);
        using var form = CreateFormWithoutFile();

        var response = await auth.Client.PostAsync("/api/clients/import-csv", form);

        await AssertBadRequestMessageAsync(response, MensajeArchivoRequerido);
    }

    [Fact]
    public async Task PostClientsImportCsv_ExtensionInvalida_Retorna400Message()
    {
        using var auth = _factory.CreateClientWithTestAuth(roles: ["Admin"]);
        using var form = CreateForm("Name;Email\nAna;ana@test.local\n", "datos.txt");

        var response = await auth.Client.PostAsync("/api/clients/import-csv", form);

        await AssertBadRequestMessageAsync(response, MensajeExtensionInvalida);
    }

    [Fact]
    public async Task PostOrdersImportCsv_SinArchivo_Retorna400ConMessage()
    {
        using var auth = _factory.CreateClientWithTestAuth(roles: ["Admin"]);
        using var form = CreateFormWithoutFile();

        var response = await auth.Client.PostAsync("/api/orders/import-csv", form);

        await AssertBadRequestMessageAsync(response, MensajeArchivoRequerido);
    }

    [Fact]
    public async Task PostOrdersImportCsv_ExtensionInvalida_Retorna400Message()
    {
        using var auth = _factory.CreateClientWithTestAuth(roles: ["Admin"]);
        using var form = CreateForm("OrderNumber;ClientId\nO1;1\n", "pedidos.txt");

        var response = await auth.Client.PostAsync("/api/orders/import-csv", form);

        await AssertBadRequestMessageAsync(response, MensajeExtensionInvalida);
    }

    [Fact]
    public async Task PostClientsImportCsv_Valido_Retorna200Resultado()
    {
        using var auth = _factory.CreateClientWithTestAuth(roles: ["Admin"]);
        var email = $"csv.contract.client.{Guid.NewGuid():N}@test.local";
        using var form = CreateForm(
            "Name;Email;Phone;Address\n" +
            $"Ana Perez;{email};600123456;Av. Central 1\n",
            "clientes.csv");

        var response = await auth.Client.PostAsync("/api/clients/import-csv", form);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = await ReadJsonAsync(response);
        AssertImportResultCounters(doc.RootElement, imported: 1);
    }

    [Fact]
    public async Task PostClientsImportCsv_DuplicadoEnArchivo_Retorna400ConErrors()
    {
        using var auth = _factory.CreateClientWithTestAuth(roles: ["Admin"]);
        var email = $"csv.duplicate.{Guid.NewGuid():N}@test.local";
        using var form = CreateForm(
            "Name;Email;Phone;Address\n" +
            $"Ana Perez;{email};600111111;Av. Central 1\n" +
            $"Berta Perez;{email};600222222;Av. Central 2\n",
            "clientes-duplicados.csv");

        var response = await auth.Client.PostAsync("/api/clients/import-csv", form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var doc = await ReadJsonAsync(response);
        var errors = AssertImportErrorsWithShape(doc.RootElement);
        Assert.Contains(errors, e =>
            string.Equals(GetProperty(e, "field").GetString(), "Email", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task PostProductsImportCsv_Valido_Retorna200Resultado()
    {
        using var auth = _factory.CreateClientWithTestAuth(roles: ["Admin"]);
        var sku = $"SKU-CSV-{Guid.NewGuid():N}";
        using var form = CreateForm(
            "Sku;Name;Category;BasePrice;Cost;Stock;MinStockAlert;Active\n" +
            $"{sku};Producto CSV;General;10.50;5.25;5;1;true\n",
            "productos.csv");

        var response = await auth.Client.PostAsync("/api/products/import-csv", form);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = await ReadJsonAsync(response);
        AssertImportResultCounters(doc.RootElement, imported: 1);
    }

    [Fact]
    public async Task PostOrdersImportCsv_ValidoConCatalogo_Retorna200Resultado()
    {
        using var auth = _factory.CreateClientWithTestAuth(roles: ["Admin"]);
        var (clientId, sellerId, productId) = await SeedCatalogAsync(auth, productActive: true);
        var orderNumber = $"CSV-{Guid.NewGuid():N}"[..20];

        using var form = CreateForm(
            "OrderNumber;ClientId;SellerId;ProductId;Cantidad\n" +
            $"{orderNumber};{clientId};{sellerId};{productId};1\n",
            "pedidos.csv");

        var response = await auth.Client.PostAsync("/api/orders/import-csv", form);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = await ReadJsonAsync(response);
        AssertImportResultCounters(doc.RootElement, imported: 1);

        var persisted = await auth.QueryDbAsync(async db =>
        {
            var order = await db.Orders.SingleAsync(o => o.OrderNumber == orderNumber);
            var product = await db.Products.SingleAsync(p => p.Id == productId);
            return (order.Estado, product.Stock);
        });
        Assert.Equal("Pendiente", persisted.Estado);
        Assert.Equal(5, persisted.Stock);
    }

    [Fact]
    public async Task PostOrdersImportCsv_ProductIdInactivo_Retorna400ConErrors()
    {
        using var auth = _factory.CreateClientWithTestAuth(roles: ["Admin"]);
        var (clientId, sellerId, productId) = await SeedCatalogAsync(auth, productActive: false);
        var orderNumber = $"CSV-{Guid.NewGuid():N}"[..20];

        using var form = CreateForm(
            "OrderNumber;ClientId;SellerId;ProductId;Cantidad\n" +
            $"{orderNumber};{clientId};{sellerId};{productId};1\n",
            "pedidos-producto-inactivo.csv");

        var response = await auth.Client.PostAsync("/api/orders/import-csv", form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var doc = await ReadJsonAsync(response);
        var errors = AssertImportErrorsWithShape(doc.RootElement);
        Assert.Contains(errors, e =>
            string.Equals(GetProperty(e, "field").GetString(), "ProductId", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task PostOrdersImportCsv_SellerIdInactivo_Retorna400ConErrors()
    {
        using var auth = _factory.CreateClientWithTestAuth(roles: ["Admin"]);
        var (clientId, sellerId, productId) = await SeedCatalogAsync(auth, productActive: true, sellerActive: false);
        var orderNumber = $"CSV-{Guid.NewGuid():N}"[..20];

        using var form = CreateForm(
            "OrderNumber;ClientId;SellerId;ProductId;Cantidad\n" +
            $"{orderNumber};{clientId};{sellerId};{productId};1\n",
            "pedidos-vendedor-inactivo.csv");

        var response = await auth.Client.PostAsync("/api/orders/import-csv", form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var doc = await ReadJsonAsync(response);
        var errors = AssertImportErrorsWithShape(doc.RootElement);
        Assert.Contains(errors, e =>
            string.Equals(GetProperty(e, "field").GetString(), "SellerId", StringComparison.OrdinalIgnoreCase)
            && GetProperty(e, "message").GetString()?.Contains("inactivo", StringComparison.OrdinalIgnoreCase) == true);
    }

    [Fact]
    public async Task PostOrdersImportCsv_OrderNumberDuplicadoEnDb_Retorna400ConErrors()
    {
        using var auth = _factory.CreateClientWithTestAuth(roles: ["Admin"]);
        var (clientId, sellerId, productId) = await SeedCatalogAsync(auth, productActive: true);
        const string orderNumber = "DUP-001";
        await SeedExistingOrderAsync(auth, clientId, sellerId, productId, orderNumber);

        using var form = CreateForm(
            "OrderNumber;ClientId;SellerId;ProductId;Cantidad\n" +
            $"{orderNumber};{clientId};{sellerId};{productId};1\n",
            "pedidos-duplicados.csv");

        var response = await auth.Client.PostAsync("/api/orders/import-csv", form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var doc = await ReadJsonAsync(response);
        var errors = AssertImportErrorsWithShape(doc.RootElement);
        Assert.Contains(errors, e =>
            string.Equals(GetProperty(e, "field").GetString(), "OrderNumber", StringComparison.OrdinalIgnoreCase)
            && GetProperty(e, "message").GetString()?.Contains("existe", StringComparison.OrdinalIgnoreCase) == true);
    }

    [Fact]
    public async Task PostOrdersImportCsv_OrderNumberDuplicadoEnArchivo_Retorna400ConErrors()
    {
        using var auth = _factory.CreateClientWithTestAuth(roles: ["Admin"]);
        var (clientId, sellerId, productId) = await SeedCatalogAsync(auth, productActive: true);
        var orderNumber = $"CSV-{Guid.NewGuid():N}"[..20];

        using var form = CreateForm(
            "OrderNumber;ClientId;SellerId;ProductId;Cantidad\n" +
            $"{orderNumber};{clientId};{sellerId};{productId};1\n" +
            $"{orderNumber};{clientId};{sellerId};{productId};2\n",
            "pedidos-duplicados-en-archivo.csv");

        var response = await auth.Client.PostAsync("/api/orders/import-csv", form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var doc = await ReadJsonAsync(response);
        var errors = AssertImportErrorsWithShape(doc.RootElement);
        Assert.Contains(errors, e =>
            string.Equals(GetProperty(e, "field").GetString(), "OrderNumber", StringComparison.OrdinalIgnoreCase)
            && GetProperty(e, "message").GetString()?.Contains("duplicado", StringComparison.OrdinalIgnoreCase) == true);
    }

    [Fact]
    public async Task PostOrdersImportCsv_OrderNumberMuyLargo_Retorna400ConErrors()
    {
        using var auth = _factory.CreateClientWithTestAuth(roles: ["Admin"]);
        var (clientId, sellerId, productId) = await SeedCatalogAsync(auth, productActive: true);
        var orderNumber = new string('X', 51);

        using var form = CreateForm(
            "OrderNumber;ClientId;SellerId;ProductId;Cantidad\n" +
            $"{orderNumber};{clientId};{sellerId};{productId};1\n",
            "pedidos-ordernumber-largo.csv");

        var response = await auth.Client.PostAsync("/api/orders/import-csv", form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var doc = await ReadJsonAsync(response);
        var errors = AssertImportErrorsWithShape(doc.RootElement);
        Assert.Contains(errors, e =>
            string.Equals(GetProperty(e, "field").GetString(), "OrderNumber", StringComparison.OrdinalIgnoreCase)
            && GetProperty(e, "message").GetString()?.Contains("50 caracteres", StringComparison.OrdinalIgnoreCase) == true);
    }

    [Fact]
    public async Task PostOrdersImportCsv_ClientIdInexistente_Retorna400ConErrors()
    {
        using var auth = _factory.CreateClientWithTestAuth(roles: ["Admin"]);
        var (_, sellerId, productId) = await SeedCatalogAsync(auth, productActive: true);
        var missingClientId = await auth.QueryDbAsync(async db =>
            (await db.Clients.Select(c => c.Id).ToListAsync()).DefaultIfEmpty(0).Max() + 1);
        var orderNumber = $"CSV-{Guid.NewGuid():N}"[..20];

        using var form = CreateForm(
            "OrderNumber;ClientId;SellerId;ProductId;Cantidad\n" +
            $"{orderNumber};{missingClientId};{sellerId};{productId};1\n",
            "pedidos-cliente-inexistente.csv");

        var response = await auth.Client.PostAsync("/api/orders/import-csv", form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var doc = await ReadJsonAsync(response);
        var errors = AssertImportErrorsWithShape(doc.RootElement);
        Assert.Contains(errors, e =>
            string.Equals(GetProperty(e, "field").GetString(), "ClientId", StringComparison.OrdinalIgnoreCase)
            && GetProperty(e, "message").GetString()?.Contains("no existe", StringComparison.OrdinalIgnoreCase) == true);
    }

    [Fact]
    public async Task PostOrdersImportCsv_ClientIdVacioOConFormatoInvalido_Retorna400ConErrors()
    {
        using var auth = _factory.CreateClientWithTestAuth(roles: ["Admin"]);
        var orderNumberVacio = $"CSV-{Guid.NewGuid():N}"[..20];
        var orderNumberTexto = $"CSV-{Guid.NewGuid():N}"[..20];

        using var form = CreateForm(
            "OrderNumber;ClientId;SellerId;ProductId;Cantidad\n" +
            $"{orderNumberVacio};;;1;1\n" +
            $"{orderNumberTexto};abc;;1;1\n",
            "pedidos-cliente-formato-invalido.csv");

        var response = await auth.Client.PostAsync("/api/orders/import-csv", form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var doc = await ReadJsonAsync(response);
        var errors = AssertImportErrorsWithShape(doc.RootElement);
        Assert.Contains(errors, e =>
            GetProperty(e, "row").GetInt32() == 2 &&
            string.Equals(GetProperty(e, "field").GetString(), "ClientId", StringComparison.OrdinalIgnoreCase) &&
            GetProperty(e, "message").GetString()?.Contains("ClientId inválido", StringComparison.OrdinalIgnoreCase) == true);
        Assert.Contains(errors, e =>
            GetProperty(e, "row").GetInt32() == 3 &&
            string.Equals(GetProperty(e, "field").GetString(), "ClientId", StringComparison.OrdinalIgnoreCase) &&
            GetProperty(e, "value").GetString() == "abc" &&
            GetProperty(e, "message").GetString()?.Contains("ClientId inválido", StringComparison.OrdinalIgnoreCase) == true);
    }

    [Fact]
    public async Task PostOrdersImportCsv_CantidadInvalida_Retorna400ConErrors()
    {
        using var auth = _factory.CreateClientWithTestAuth(roles: ["Admin"]);
        var (clientId, sellerId, productId) = await SeedCatalogAsync(auth, productActive: true);
        var orderNumber = $"CSV-{Guid.NewGuid():N}"[..20];

        using var form = CreateForm(
            "OrderNumber;ClientId;SellerId;ProductId;Cantidad\n" +
            $"{orderNumber};{clientId};{sellerId};{productId};0\n",
            "pedidos-cantidad-invalida.csv");

        var response = await auth.Client.PostAsync("/api/orders/import-csv", form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var doc = await ReadJsonAsync(response);
        var errors = AssertImportErrorsWithShape(doc.RootElement);
        Assert.Contains(errors, e =>
            string.Equals(GetProperty(e, "field").GetString(), "Cantidad", StringComparison.OrdinalIgnoreCase)
            && GetProperty(e, "message").GetString()?.Contains("Cantidad inválida", StringComparison.OrdinalIgnoreCase) == true);
    }

    [Fact]
    public async Task PostOrdersImportCsv_SellerIdInexistente_Retorna400ConErrors()
    {
        using var auth = _factory.CreateClientWithTestAuth(roles: ["Admin"]);
        var (clientId, _, productId) = await SeedCatalogAsync(auth, productActive: true);
        var missingSellerId = await auth.QueryDbAsync(async db =>
            (await db.Sellers.Select(s => s.Id).ToListAsync()).DefaultIfEmpty(0).Max() + 1);
        var orderNumber = $"CSV-{Guid.NewGuid():N}"[..20];

        using var form = CreateForm(
            "OrderNumber;ClientId;SellerId;ProductId;Cantidad\n" +
            $"{orderNumber};{clientId};{missingSellerId};{productId};1\n",
            "pedidos-vendedor-inexistente.csv");

        var response = await auth.Client.PostAsync("/api/orders/import-csv", form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var doc = await ReadJsonAsync(response);
        var errors = AssertImportErrorsWithShape(doc.RootElement);
        Assert.Contains(errors, e =>
            string.Equals(GetProperty(e, "field").GetString(), "SellerId", StringComparison.OrdinalIgnoreCase)
            && GetProperty(e, "message").GetString()?.Contains("no existe", StringComparison.OrdinalIgnoreCase) == true);
    }

    [Fact]
    public async Task PostOrdersImportCsv_ProductIdInexistente_Retorna400ConErrors()
    {
        using var auth = _factory.CreateClientWithTestAuth(roles: ["Admin"]);
        var (clientId, sellerId, _) = await SeedCatalogAsync(auth, productActive: true);
        var missingProductId = await auth.QueryDbAsync(async db =>
            (await db.Products.Select(p => p.Id).ToListAsync()).DefaultIfEmpty(0).Max() + 1);
        var orderNumber = $"CSV-{Guid.NewGuid():N}"[..20];

        using var form = CreateForm(
            "OrderNumber;ClientId;SellerId;ProductId;Cantidad\n" +
            $"{orderNumber};{clientId};{sellerId};{missingProductId};1\n",
            "pedidos-producto-inexistente.csv");

        var response = await auth.Client.PostAsync("/api/orders/import-csv", form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var doc = await ReadJsonAsync(response);
        var errors = AssertImportErrorsWithShape(doc.RootElement);
        Assert.Contains(errors, e =>
            string.Equals(GetProperty(e, "field").GetString(), "ProductId", StringComparison.OrdinalIgnoreCase)
            && GetProperty(e, "message").GetString()?.Contains("no existe", StringComparison.OrdinalIgnoreCase) == true);
    }

    [Fact]
    public async Task PostClientsImportCsv_HeadersFaltantes_Retorna400ConErrors()
    {
        using var auth = _factory.CreateClientWithTestAuth(roles: ["Admin"]);
        using var form = CreateForm("Name;Email\nAna;ana@test.local\n", "clientes-sin-columnas.csv");

        var response = await auth.Client.PostAsync("/api/clients/import-csv", form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var doc = await ReadJsonAsync(response);
        var errors = AssertImportErrorsWithShape(doc.RootElement);
        Assert.Contains(errors, e =>
            string.Equals(GetProperty(e, "field").GetString(), "header", StringComparison.OrdinalIgnoreCase)
            && GetProperty(e, "message").GetString()?.Contains("Faltan columnas requeridas", StringComparison.OrdinalIgnoreCase) == true);
    }

    [Fact]
    public async Task PostProductsImportCsv_ArchivoVacio_Retorna400ConErrors()
    {
        using var auth = _factory.CreateClientWithTestAuth(roles: ["Admin"]);
        using var form = CreateForm("\n", "productos-vacio.csv");

        var response = await auth.Client.PostAsync("/api/products/import-csv", form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var doc = await ReadJsonAsync(response);
        var errors = AssertImportErrorsWithShape(doc.RootElement);
        Assert.Contains(errors, e =>
            string.Equals(GetProperty(e, "field").GetString(), "file", StringComparison.OrdinalIgnoreCase)
            && GetProperty(e, "message").GetString()?.Contains("vacío", StringComparison.OrdinalIgnoreCase) == true);
    }

    [Theory]
    [InlineData("/api/products/import-csv", "Sku;Name;Category;BasePrice;Cost;Stock;MinStockAlert;Active\n", "productos-solo-header.csv")]
    [InlineData("/api/clients/import-csv", "Name;Email;Phone;Address\n", "clientes-solo-header.csv")]
    public async Task PostImportCsv_SoloHeader_Retorna400ConErrors(string url, string contenido, string fileName)
    {
        using var auth = _factory.CreateClientWithTestAuth(roles: ["Admin"]);
        using var form = CreateForm(contenido, fileName);

        var response = await auth.Client.PostAsync(url, form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var doc = await ReadJsonAsync(response);
        var errors = AssertImportErrorsWithShape(doc.RootElement);
        Assert.Contains(errors, e =>
            GetProperty(e, "row").GetInt32() == 0 &&
            string.Equals(GetProperty(e, "field").GetString(), "file", StringComparison.OrdinalIgnoreCase) &&
            GetProperty(e, "message").GetString()?.Contains("no contiene filas de datos", StringComparison.OrdinalIgnoreCase) == true);
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

    private static void AssertImportResultCounters(JsonElement root, int imported)
    {
        Assert.True(GetProperty(root, "totalRows").GetInt32() >= 1);
        Assert.Equal(imported, GetProperty(root, "validRows").GetInt32());
        Assert.Equal(imported, GetProperty(root, "imported").GetInt32());
        var errors = GetProperty(root, "errors");
        Assert.Equal(JsonValueKind.Array, errors.ValueKind);
        Assert.Empty(errors.EnumerateArray());
    }

    private static JsonElement[] AssertImportErrorsWithShape(JsonElement root)
    {
        var errors = GetProperty(root, "errors");
        Assert.Equal(JsonValueKind.Array, errors.ValueKind);
        var items = errors.EnumerateArray().ToArray();
        Assert.NotEmpty(items);
        foreach (var error in items)
        {
            Assert.True(GetProperty(error, "row").TryGetInt32(out _));
            Assert.False(string.IsNullOrWhiteSpace(GetProperty(error, "field").GetString()));
            Assert.NotNull(GetProperty(error, "value").GetString());
            Assert.False(string.IsNullOrWhiteSpace(GetProperty(error, "message").GetString()));
        }
        return items;
    }

    private static async Task<int> SeedExistingOrderAsync(
        TestAuthClient auth, int clientId, int sellerId, int productId, string orderNumber) =>
        await auth.QueryDbAsync(async db =>
        {
            var order = Order.Rehydrate(
                id: 0,
                clientId: clientId,
                sellerId: sellerId,
                productId: productId,
                productName: "Pedido existente CSV",
                cantidad: 1,
                precioUnitario: 10.50m,
                total: 10.50m,
                fecha: DateTime.UtcNow,
                estado: "Pendiente",
                orderNumber: orderNumber);
            db.Orders.Add(order);
            await db.SaveChangesAsync();
            return order.Id;
        });

    private static async Task<(int ClientId, int SellerId, int ProductId)> SeedCatalogAsync(
        TestAuthClient auth, bool productActive, bool sellerActive = true)
    {
        var suffix = Guid.NewGuid().ToString("N");
        return await auth.QueryDbAsync(async db =>
        {
            var client = Client.Create($"Cliente CSV {suffix}", $"csv.contract.client.{suffix}@test.local");
            var seller = Seller.Create($"Vendedor CSV {suffix}", $"csv.contract.seller.{suffix}@test.local", active: sellerActive);
            var product = Product.Create(
                $"SKU-CSV-{suffix}",
                $"Producto CSV {suffix}",
                "General",
                10.50m,
                5.25m,
                5,
                1,
                productActive);
            db.Clients.Add(client);
            db.Sellers.Add(seller);
            db.Products.Add(product);
            await db.SaveChangesAsync();
            return (client.Id, seller.Id, product.Id);
        });
    }
}
