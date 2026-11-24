using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NeoOrders.Application.Contracts.Clients;
using NeoOrders.Application.Contracts.Products;
using NeoOrders.Application.Contracts.Sellers;
using NeoOrders.Application.DTOs.Clients;
using NeoOrders.Application.DTOs.Products;
using NeoOrders.Application.DTOs.Productos;
using NeoOrders.Application.DTOs.Sellers;
using NeoOrders.Infrastructure.Models;

namespace NeoOrders.Api.Tests;

/// <summary>
/// KAN-41/KAN-42: cierre de cobertura de contratos HTTP de alto valor que aún
/// no estaban cubiertos. Todos los casos afirman el comportamiento REAL del
/// producto (sin cambios productivos): cuando el proveedor InMemory no aplica
/// índices únicos se usan stubs, igual que en <see cref="ErrorContractTests"/>.
/// Cada cliente autenticado crea una factory derivada con DB aislada.
/// </summary>
public class ContractCloseoutTests : IClassFixture<NeoOrdersApiFactory>
{
    private readonly NeoOrdersApiFactory _factory;

    public ContractCloseoutTests(NeoOrdersApiFactory factory)
    {
        _factory = factory;
    }

    // ---------------- A) Orders ----------------

    /// <summary>
    /// KAN-39: el alta directa aplica la misma regla que la importación CSV y
    /// rechaza vendedores inactivos con el contrato de error habitual {message}.
    /// </summary>
    [Fact]
    public async Task PostOrder_VendedorInactivo_Retorna400Message()
    {
        using var auth = _factory.CreateClientWithTestAuth();
        var clientId = await SeedClientAsync(auth);
        var productId = await SeedProductAsync(auth, active: true, stock: 5);
        var inactiveSellerId = await SeedSellerAsync(auth, active: false);

        var response = await PostOrderAsync(auth.Client, clientId, inactiveSellerId, productId);

        await AssertBadRequestMessageAsync(response, "El vendedor está inactivo.");
        Assert.True(await auth.QueryDbAsync(async db => !await db.Orders.AnyAsync()));
    }

    /// <summary>
    /// KAN-39: igual que el vendedor, un producto inactivo se rechaza también
    /// en el alta directa (antes sólo lo rechazaba el importador CSV).
    /// </summary>
    [Fact]
    public async Task PostOrder_ProductoInactivo_Retorna400Message()
    {
        using var auth = _factory.CreateClientWithTestAuth();
        var clientId = await SeedClientAsync(auth);
        var sellerId = await SeedSellerAsync(auth, active: true);
        var inactiveProductId = await SeedProductAsync(auth, active: false, stock: 5);

        var response = await PostOrderAsync(auth.Client, clientId, sellerId, inactiveProductId);

        await AssertBadRequestMessageAsync(response, "El producto está inactivo.");
        Assert.True(await auth.QueryDbAsync(async db => !await db.Orders.AnyAsync()));
    }

    /// <summary>
    /// UpdateAsync no lee dto.ProductId: ni un ID inexistente ni el de otro
    /// producto (aquí inactivo) alteran el producto/precio del pedido pendiente.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PutOrder_PendienteConOtroProductId_ContratoObservadoIgnoraProducto(bool missingProductId)
    {
        using var auth = _factory.CreateClientWithTestAuth();
        var clientId = await SeedClientAsync(auth);
        var sellerId = await SeedSellerAsync(auth, active: true);
        var productId = await SeedProductAsync(auth, active: true, stock: 5);
        var orderId = await CreatePendingOrderAsync(auth, clientId, sellerId, productId);
        var otherProductId = missingProductId
            ? await MissingProductIdAsync(auth)
            : await SeedProductAsync(auth, active: false, stock: 5);

        var response = await auth.Client.PutAsJsonAsync($"/api/orders/{orderId}", new
        {
            id = orderId,
            clientId,
            sellerId,
            productId = otherProductId,
            productName = "Producto contrato",
            cantidad = 1,
            precioUnitario = 10.50m,
            estado = "Pendiente"
        });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var persisted = await auth.QueryDbAsync(async db =>
        {
            var order = (await db.Orders.FindAsync(orderId))!;
            return (order.ProductId, order.PrecioUnitario, order.Total);
        });
        Assert.Equal(productId, persisted.ProductId);
        Assert.Equal(10.50m, persisted.PrecioUnitario);
        Assert.Equal(10.50m, persisted.Total);
    }

    [Fact]
    public async Task PatchOrderStatus_EntregadoAProcesado_Retorna400YConservaEstado()
    {
        using var auth = _factory.CreateClientWithTestAuth();
        var clientId = await SeedClientAsync(auth);
        var sellerId = await SeedSellerAsync(auth, active: true);
        var productId = await SeedProductAsync(auth, active: true, stock: 5);
        var orderId = await CreatePendingOrderAsync(auth, clientId, sellerId, productId);

        Assert.Equal(HttpStatusCode.NoContent, (await PatchStatusAsync(auth.Client, orderId, "Procesado")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await PatchStatusAsync(auth.Client, orderId, "Enviado")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await PatchStatusAsync(auth.Client, orderId, "Entregado")).StatusCode);

        var response = await PatchStatusAsync(auth.Client, orderId, "Procesado");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var doc = await ReadJsonAsync(response);
        var message = GetProperty(doc.RootElement, "message").GetString() ?? string.Empty;
        Assert.Contains("Transici", message, StringComparison.Ordinal);
        Assert.Contains("Entregado", message, StringComparison.Ordinal);
        Assert.Contains("Procesado", message, StringComparison.Ordinal);
        Assert.Equal("Entregado", await auth.QueryDbAsync(async db => (await db.Orders.FindAsync(orderId))!.Estado));
    }

    // ---------------- B) Products ----------------

    [Fact]
    public async Task PostProduct_SkuDuplicado_Retorna400Message()
    {
        using var auth = _factory.CreateClientWithTestAuth(
            roles: ["Admin"],
            configureServices: services =>
            {
                services.RemoveAll<IProductService>();
                services.AddScoped<IProductService, DuplicateSkuProductService>();
            });

        var response = await auth.Client.PostAsJsonAsync("/api/products", ValidProductPayload(0, "SKU-DUPLICADO"));

        await AssertBadRequestMessageAsync(response, "Ya existe un producto con ese SKU.");
    }

    [Fact]
    public async Task PutProduct_SkuDuplicado_Retorna400Message()
    {
        using var auth = _factory.CreateClientWithTestAuth(
            roles: ["Admin"],
            configureServices: services =>
            {
                services.RemoveAll<IProductService>();
                services.AddScoped<IProductService, DuplicateSkuProductService>();
            });

        var response = await auth.Client.PutAsJsonAsync("/api/products/15", ValidProductPayload(15, "SKU-DUPLICADO"));

        await AssertBadRequestMessageAsync(response, "Ya existe un producto con ese SKU.");
    }

    /// <summary>
    /// ApplyPriceAdjustmentAsync lanza InvalidOperationException cuando factor &lt;= 0;
    /// el controller lo mapea a 400 {message}. Preview, en cambio, devuelve 200
    /// con errors[]; aquí se cubre el endpoint apply (contrato real observado).
    /// </summary>
    [Theory]
    [InlineData(-100)]
    [InlineData(-150)]
    public async Task ApplyPriceAdjustment_FactorNoPositivo_Retorna400Message(int percent)
    {
        using var auth = _factory.CreateClientWithTestAuth(roles: ["Admin"]);

        var response = await auth.Client.PostAsJsonAsync(
            "/api/products/price-adjustment/apply", new { percent });

        await AssertBadRequestMessageAsync(response, "El factor resultante debe ser mayor a 0.");
    }

    // ---------------- C) Clients / Sellers ----------------

    [Fact]
    public async Task PutClient_EmailDuplicado_Retorna400Message()
    {
        using var auth = _factory.CreateClientWithTestAuth(
            roles: ["Admin"],
            configureServices: services =>
            {
                services.RemoveAll<IClientService>();
                services.AddScoped<IClientService, DuplicateEmailClientService>();
            });

        var response = await auth.Client.PutAsJsonAsync("/api/clients/9", new
        {
            id = 9,
            nombre = "Cliente duplicado",
            email = "cliente.duplicado@test.local"
        });

        await AssertBadRequestMessageAsync(response, "Ya existe un cliente con ese email.");
    }

    [Fact]
    public async Task PutSeller_EmailDuplicado_Retorna400Message()
    {
        using var auth = _factory.CreateClientWithTestAuth(
            roles: ["Admin"],
            configureServices: services =>
            {
                services.RemoveAll<ISellerService>();
                services.AddScoped<ISellerService, DuplicateEmailSellerService>();
            });

        var response = await auth.Client.PutAsJsonAsync("/api/sellers/9", new
        {
            id = 9,
            name = "Vendedor duplicado",
            email = "vendedor.duplicado@test.local"
        });

        await AssertBadRequestMessageAsync(response, "Ya existe un vendedor con ese email.");
    }

    /// <summary>
    /// La guardia de ClientService.DeleteAsync es explícita (Include(Orders)),
    /// así que no depende de que el proveedor InMemory aplique FK: es confiable.
    /// </summary>
    [Fact]
    public async Task DeleteClient_ConPedidosAsociados_Retorna400Message()
    {
        using var auth = _factory.CreateClientWithTestAuth(roles: ["Admin"]);
        var clientId = await SeedClientAsync(auth);
        var sellerId = await SeedSellerAsync(auth, active: true);
        var productId = await SeedProductAsync(auth, active: true, stock: 5);
        await CreatePendingOrderAsync(auth, clientId, sellerId, productId);

        var response = await auth.Client.DeleteAsync($"/api/clients/{clientId}");

        await AssertBadRequestMessageAsync(response, "El cliente tiene pedidos asociados");
        Assert.True(await auth.QueryDbAsync(async db => await db.Clients.AnyAsync(c => c.Id == clientId)));
    }

    [Fact]
    public async Task DeleteSeller_ConPedidosAsociados_Retorna400Message()
    {
        using var auth = _factory.CreateClientWithTestAuth(roles: ["Admin"]);
        var clientId = await SeedClientAsync(auth);
        var sellerId = await SeedSellerAsync(auth, active: true);
        var productId = await SeedProductAsync(auth, active: true, stock: 5);
        await CreatePendingOrderAsync(auth, clientId, sellerId, productId);

        var response = await auth.Client.DeleteAsync($"/api/sellers/{sellerId}");

        await AssertBadRequestMessageAsync(response, "El vendedor tiene pedidos asociados");
        Assert.True(await auth.QueryDbAsync(async db => await db.Sellers.AnyAsync(s => s.Id == sellerId)));
    }

    // ---------------- D) Auth / Users / Config ----------------

    [Fact]
    public async Task PostAuthLogout_ConAuth_Retorna204()
    {
        using var auth = _factory.CreateClientWithTestAuth();

        var response = await auth.Client.PostAsync("/api/auth/logout", null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    /// <summary>
    /// Regresión de la guardia agregada en KAN-41: un rol vacío/espacios no debe
    /// remover los roles actuales antes de fallar.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task PutUsersRol_RolVacio_Retorna400YConservaRoles(string rol)
    {
        using var auth = _factory.CreateClientWithTestAuth(roles: ["Admin"]);
        var suffix = Guid.NewGuid().ToString("N");
        var id = $"users-rol-vacio-{suffix}";
        var email = $"users.rol.vacio.{suffix}@test.local";
        await auth.SeedUserAsync(new ApplicationUser
        {
            Id = id,
            UserName = email,
            Email = email,
            EmailConfirmed = true
        }, "Empleado");

        var response = await auth.Client.PutAsJsonAsync($"/api/users/{id}/rol", new { rol });

        await AssertBadRequestMessageAsync(response, "Rol no válido.");
        var usuarios = await auth.Client.GetAsync("/api/users");
        Assert.Equal(HttpStatusCode.OK, usuarios.StatusCode);
        using var doc = await ReadJsonAsync(usuarios);
        var usuario = Assert.Single(doc.RootElement.EnumerateArray(),
            u => GetProperty(u, "id").GetString() == id);
        Assert.Contains("Empleado", GetProperty(usuario, "roles").EnumerateArray().Select(r => r.GetString()));
    }

    // ---------------- E) CSV import ----------------

    [Fact]
    public async Task PostProductsImportCsv_SkuDuplicadoEnArchivo_Retorna400ConErrors()
    {
        using var auth = _factory.CreateClientWithTestAuth(roles: ["Admin"]);
        var sku = $"SKU-CSV-DUP-FILE-{Guid.NewGuid():N}";
        using var form = CreateForm(
            "Sku;Name;Category;BasePrice;Cost;Stock;MinStockAlert;Active\n" +
            $"{sku};Producto uno;General;10.50;5.25;5;1;true\n" +
            $"{sku};Producto dos;General;11.50;6.25;5;1;true\n",
            "productos-sku-duplicado.csv");

        var response = await auth.Client.PostAsync("/api/products/import-csv", form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var doc = await ReadJsonAsync(response);
        var errors = AssertImportErrorsWithShape(doc.RootElement);
        Assert.Contains(errors, e =>
            string.Equals(GetProperty(e, "field").GetString(), "Sku", StringComparison.OrdinalIgnoreCase) &&
            GetProperty(e, "message").GetString()?.Contains("duplicado en el archivo", StringComparison.OrdinalIgnoreCase) == true);
    }

    [Fact]
    public async Task PostProductsImportCsv_SkuDuplicadoEnDb_Retorna400ConErrors()
    {
        using var auth = _factory.CreateClientWithTestAuth(roles: ["Admin"]);
        var sku = $"SKU-CSV-DUP-DB-{Guid.NewGuid():N}";
        await SeedProductAsync(auth, active: true, stock: 5, sku: sku);
        using var form = CreateForm(
            "Sku;Name;Category;BasePrice;Cost;Stock;MinStockAlert;Active\n" +
            $"{sku};Producto CSV;General;10.50;5.25;5;1;true\n",
            "productos-sku-en-db.csv");

        var response = await auth.Client.PostAsync("/api/products/import-csv", form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var doc = await ReadJsonAsync(response);
        var errors = AssertImportErrorsWithShape(doc.RootElement);
        Assert.Contains(errors, e =>
            string.Equals(GetProperty(e, "field").GetString(), "Sku", StringComparison.OrdinalIgnoreCase) &&
            GetProperty(e, "message").GetString()?.Contains("base de datos", StringComparison.OrdinalIgnoreCase) == true);
    }

    [Fact]
    public async Task PostClientsImportCsv_EmailDuplicadoEnDb_Retorna400ConErrors()
    {
        using var auth = _factory.CreateClientWithTestAuth(roles: ["Admin"]);
        var email = $"csv.dup.db.{Guid.NewGuid():N}@test.local";
        await SeedClientAsync(auth, email: email);
        using var form = CreateForm(
            "Name;Email;Phone;Address\n" +
            $"Ana Perez;{email};600123456;Av. Central 1\n",
            "clientes-email-en-db.csv");

        var response = await auth.Client.PostAsync("/api/clients/import-csv", form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var doc = await ReadJsonAsync(response);
        var errors = AssertImportErrorsWithShape(doc.RootElement);
        Assert.Contains(errors, e =>
            string.Equals(GetProperty(e, "field").GetString(), "Email", StringComparison.OrdinalIgnoreCase) &&
            GetProperty(e, "message").GetString()?.Contains("base de datos", StringComparison.OrdinalIgnoreCase) == true);
    }

    [Fact]
    public async Task PostOrdersImportCsv_OrderNumberVacio_Retorna400ConErrors()
    {
        using var auth = _factory.CreateClientWithTestAuth(roles: ["Admin"]);
        var (clientId, sellerId, productId) = await SeedCatalogAsync(auth);
        using var form = CreateForm(
            "OrderNumber;ClientId;SellerId;ProductId;Cantidad\n" +
            $";{clientId};{sellerId};{productId};1\n",
            "pedidos-ordernumber-vacio.csv");

        var response = await auth.Client.PostAsync("/api/orders/import-csv", form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var doc = await ReadJsonAsync(response);
        var errors = AssertImportErrorsWithShape(doc.RootElement);
        Assert.Contains(errors, e =>
            GetProperty(e, "row").GetInt32() == 2 &&
            string.Equals(GetProperty(e, "field").GetString(), "OrderNumber", StringComparison.OrdinalIgnoreCase) &&
            GetProperty(e, "message").GetString()?.Contains("obligatorio", StringComparison.OrdinalIgnoreCase) == true);
    }

    // ---------------- Helpers ----------------

    private static object ValidProductPayload(int id, string sku) => new
    {
        id,
        sku,
        name = "Producto contrato",
        category = "General",
        basePrice = 10.50m,
        cost = 5.25m,
        stock = 5,
        minStockAlert = 1,
        active = true
    };

    private static MultipartFormDataContent CreateForm(string content, string fileName)
    {
        var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(Encoding.UTF8.GetBytes(content));
        file.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        form.Add(file, "file", fileName);
        return form;
    }

    private static async Task<int> SeedClientAsync(TestAuthClient auth, string? email = null)
    {
        var suffix = Guid.NewGuid().ToString("N");
        return await auth.QueryDbAsync(async db =>
        {
            var client = Client.Create($"Cliente cierre {suffix}", email ?? $"cierre.client.{suffix}@test.local");
            db.Clients.Add(client);
            await db.SaveChangesAsync();
            return client.Id;
        });
    }

    private static async Task<int> SeedSellerAsync(TestAuthClient auth, bool active)
    {
        var suffix = Guid.NewGuid().ToString("N");
        return await auth.QueryDbAsync(async db =>
        {
            var seller = Seller.Create($"Vendedor cierre {suffix}", $"cierre.seller.{suffix}@test.local", active: active);
            db.Sellers.Add(seller);
            await db.SaveChangesAsync();
            return seller.Id;
        });
    }

    private static async Task<int> SeedProductAsync(TestAuthClient auth, bool active, int stock, string? sku = null)
    {
        var suffix = Guid.NewGuid().ToString("N");
        return await auth.QueryDbAsync(async db =>
        {
            var product = Product.Create(
                sku ?? $"SKU-CIERRE-{suffix}",
                $"Producto cierre {suffix}",
                "General",
                10.50m,
                5.25m,
                stock,
                1,
                active);
            db.Products.Add(product);
            await db.SaveChangesAsync();
            return product.Id;
        });
    }

    private static async Task<(int ClientId, int SellerId, int ProductId)> SeedCatalogAsync(TestAuthClient auth)
    {
        var clientId = await SeedClientAsync(auth);
        var sellerId = await SeedSellerAsync(auth, active: true);
        var productId = await SeedProductAsync(auth, active: true, stock: 5);
        return (clientId, sellerId, productId);
    }

    private static Task<int> MissingProductIdAsync(TestAuthClient auth) =>
        auth.QueryDbAsync(async db =>
            (await db.Products.Select(p => p.Id).ToListAsync()).DefaultIfEmpty(0).Max() + 1);

    private static Task<HttpResponseMessage> PostOrderAsync(
        HttpClient client, int clientId, int? sellerId, int? productId) =>
        client.PostAsJsonAsync("/api/orders", new
        {
            clientId,
            sellerId,
            productId,
            productName = "Producto contrato",
            cantidad = 1,
            precioUnitario = 10.50m
        });

    private static async Task<int> CreatePendingOrderAsync(
        TestAuthClient auth, int clientId, int? sellerId, int? productId)
    {
        var response = await PostOrderAsync(auth.Client, clientId, sellerId, productId);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var doc = await ReadJsonAsync(response);
        return GetProperty(doc.RootElement, "id").GetInt32();
    }

    private static Task<HttpResponseMessage> PatchStatusAsync(HttpClient client, int orderId, string estado) =>
        client.PatchAsJsonAsync($"/api/orders/{orderId}/estado", new { estado });

    private static async Task AssertBadRequestMessageAsync(HttpResponseMessage response, string expectedMessage)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var doc = await ReadJsonAsync(response);
        var root = doc.RootElement;
        Assert.True(root.TryGetProperty("message", out var message),
            $"La respuesta 400 no contiene 'message': {root.GetRawText()}");
        Assert.Equal(expectedMessage, message.GetString());
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

    // Stubs: el InMemory provider no aplica índices únicos (ver ErrorContractTests).

    private sealed class DuplicateSkuProductService : IProductService
    {
        public Task<List<ProductDto>> ListAsync(string? search, string? category, bool? onlyInStock, string? sku = null, string? nombre = null) =>
            Task.FromResult(new List<ProductDto>());
        public Task<List<string>> CategoriesAsync() => Task.FromResult(new List<string>());
        public Task<ProductDto?> GetByIdAsync(int id) => Task.FromResult<ProductDto?>(null);
        public Task<ProductDto> CreateAsync(ProductDto dto) =>
            throw new InvalidOperationException("Ya existe un producto con ese SKU.");
        public Task<bool> UpdateAsync(int id, ProductDto dto) =>
            throw new InvalidOperationException("Ya existe un producto con ese SKU.");
        public Task<bool> DeleteAsync(int id) => Task.FromResult(false);
        public Task<(bool Ok, string? Error, string? ImageUrl)> UploadImageAsync(
            int id, byte[] content, string fileName, string? contentType, CancellationToken ct) =>
            Task.FromResult<(bool, string?, string?)>((false, "NOT_FOUND", null));
        public Task<(bool Ok, string? Error)> RemoveImageAsync(int id, CancellationToken ct) =>
            Task.FromResult<(bool, string?)>((false, "NOT_FOUND"));
        public Task<PriceAdjustmentPreviewResponse> PreviewPriceAdjustmentAsync(decimal percent) =>
            Task.FromResult(new PriceAdjustmentPreviewResponse());
        public Task<PriceAdjustmentApplyResponse> ApplyPriceAdjustmentAsync(decimal percent) =>
            Task.FromResult(new PriceAdjustmentApplyResponse());
    }

    private sealed class DuplicateEmailClientService : IClientService
    {
        public Task<List<ClientListItemDto>> ListAsync(string? search) =>
            Task.FromResult(new List<ClientListItemDto>());
        public Task<ClientDto?> GetByIdAsync(int id) => Task.FromResult<ClientDto?>(null);
        public Task<ClientDto> CreateAsync(ClientDto dto) =>
            throw new InvalidOperationException("Ya existe un cliente con ese email.");
        public Task<bool> UpdateAsync(int id, ClientDto dto) =>
            throw new InvalidOperationException("Ya existe un cliente con ese email.");
        public Task<(bool Ok, string? Error)> DeleteAsync(int id) =>
            Task.FromResult((false, (string?)null));
    }

    private sealed class DuplicateEmailSellerService : ISellerService
    {
        public Task<List<SellerDto>> ListAsync(bool? onlyActive) => Task.FromResult(new List<SellerDto>());
        public Task<SellerDto?> GetByIdAsync(int id) => Task.FromResult<SellerDto?>(null);
        public Task<SellerDto> CreateAsync(SellerDto dto) =>
            throw new InvalidOperationException("Ya existe un vendedor con ese email.");
        public Task<bool> UpdateAsync(int id, SellerDto dto) =>
            throw new InvalidOperationException("Ya existe un vendedor con ese email.");
        public Task<(bool Ok, string? Error)> DeleteAsync(int id) =>
            Task.FromResult((false, (string?)null));
    }
}
