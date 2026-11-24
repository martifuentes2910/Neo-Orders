using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace NeoOrders.Api.Tests;

/// <summary>
/// Smoke/contract tests de /api/orders: flujos felices y errores de negocio
/// clave. Cada test usa su propio cliente autenticado y, por lo tanto, su
/// propia base InMemory aislada (Seed:RunOnStartup=false).
/// </summary>
public class OrdersContractTests : IClassFixture<NeoOrdersApiFactory>
{
    private readonly NeoOrdersApiFactory _factory;

    public OrdersContractTests(NeoOrdersApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PostOrder_DatosValidos_Retorna201ConId()
    {
        using var auth = _factory.CreateClientWithTestAuth();
        var (clientId, sellerId, productId) = await SeedCatalogAsync(auth);

        var response = await PostOrderAsync(auth.Client, clientId, sellerId, productId);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var doc = await ReadJsonAsync(response);
        Assert.True(GetProperty(doc.RootElement, "id").GetInt32() > 0);
    }

    [Fact]
    public async Task PostOrder_ClienteInvalido_Retorna400Message()
    {
        using var auth = _factory.CreateClientWithTestAuth();
        var productId = await SeedProductAsync(auth);

        var response = await PostOrderAsync(auth.Client, 999999, null, productId);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var doc = await ReadJsonAsync(response);
        Assert.Equal("Cliente no válido", GetProperty(doc.RootElement, "message").GetString());
    }

    [Fact]
    public async Task PostOrder_SinProducto_Retorna400Message()
    {
        using var auth = _factory.CreateClientWithTestAuth();
        var clientId = await SeedClientAsync(auth);

        var response = await PostOrderAsync(auth.Client, clientId, null, null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var doc = await ReadJsonAsync(response);
        Assert.Equal("Debe seleccionar un producto del catálogo", GetProperty(doc.RootElement, "message").GetString());
    }

    [Fact]
    public async Task PostOrder_CantidadInvalida_Retorna400Message()
    {
        using var auth = _factory.CreateClientWithTestAuth();
        var (clientId, sellerId, productId) = await SeedCatalogAsync(auth);

        var response = await PostOrderAsync(auth.Client, clientId, sellerId, productId, cantidad: 0);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var doc = await ReadJsonAsync(response);
        Assert.Equal("Cantidad inválida", GetProperty(doc.RootElement, "message").GetString());
    }

    [Fact]
    public async Task PostOrder_OrderNumberDuplicado_Retorna400Message()
    {
        using var auth = _factory.CreateClientWithTestAuth();
        var (clientId, sellerId, productId) = await SeedCatalogAsync(auth);

        var first = await PostOrderAsync(auth.Client, clientId, sellerId, productId, orderNumber: "ON-001");
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await PostOrderAsync(auth.Client, clientId, sellerId, productId, orderNumber: "ON-001");

        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
        using var doc = await ReadJsonAsync(second);
        Assert.Equal("OrderNumber ya existe", GetProperty(doc.RootElement, "message").GetString());
    }

    [Fact]
    public async Task PostOrder_OrderNumberMuyLargo_Retorna400Message()
    {
        using var auth = _factory.CreateClientWithTestAuth();
        var (clientId, sellerId, productId) = await SeedCatalogAsync(auth);
        var orderNumber = new string('X', 51);

        var response = await PostOrderAsync(auth.Client, clientId, sellerId, productId, orderNumber: orderNumber);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var doc = await ReadJsonAsync(response);
        Assert.Equal("OrderNumber no puede superar 50 caracteres", GetProperty(doc.RootElement, "message").GetString());
    }

    [Fact]
    public async Task PostOrder_StockInsuficienteAlProcesar_Retorna400Message()
    {
        using var auth = _factory.CreateClientWithTestAuth();
        var (clientId, sellerId, productId) = await SeedCatalogAsync(auth, productStock: 1);
        var orderId = await CreatePendingOrderIdAsync(auth, clientId, sellerId, productId, cantidad: 2);

        var response = await auth.Client.PatchAsJsonAsync(
            $"/api/orders/{orderId}/estado", new { estado = "Procesado" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var doc = await ReadJsonAsync(response);
        var message = GetProperty(doc.RootElement, "message").GetString() ?? string.Empty;
        Assert.Contains("Stock insuficiente", message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PatchOrderStatus_EstadoInvalido_Retorna400Message()
    {
        using var auth = _factory.CreateClientWithTestAuth();
        var (clientId, sellerId, productId) = await SeedCatalogAsync(auth);
        var orderId = await CreatePendingOrderIdAsync(auth, clientId, sellerId, productId);

        var response = await auth.Client.PatchAsJsonAsync(
            $"/api/orders/{orderId}/estado", new { estado = "Inexistente" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var doc = await ReadJsonAsync(response);
        Assert.Equal("Estado no válido", GetProperty(doc.RootElement, "message").GetString());
    }

    [Fact]
    public async Task PatchOrderStatus_TransicionValida_Retorna204()
    {
        using var auth = _factory.CreateClientWithTestAuth();
        var (clientId, sellerId, productId) = await SeedCatalogAsync(auth, productStock: 5);
        var orderId = await CreatePendingOrderIdAsync(auth, clientId, sellerId, productId);

        var response = await auth.Client.PatchAsJsonAsync(
            $"/api/orders/{orderId}/estado", new { estado = "Procesado" });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task PatchOrderStatus_TransicionInvalida_Retorna400Message()
    {
        using var auth = _factory.CreateClientWithTestAuth();
        var (clientId, sellerId, productId) = await SeedCatalogAsync(auth);
        var orderId = await CreatePendingOrderIdAsync(auth, clientId, sellerId, productId);

        var response = await auth.Client.PatchAsJsonAsync(
            $"/api/orders/{orderId}/estado", new { estado = "Entregado" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var doc = await ReadJsonAsync(response);
        var message = GetProperty(doc.RootElement, "message").GetString() ?? string.Empty;
        Assert.False(string.IsNullOrWhiteSpace(message));
        // El contrato real es "Transición de estado no permitida: Pendiente → Entregado".
        // Se asertan las partes ASCII para no acoplar el test a la codificación.
        Assert.StartsWith("Transici", message, StringComparison.Ordinal);
        Assert.Contains("Pendiente", message, StringComparison.Ordinal);
        Assert.Contains("Entregado", message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DeleteOrder_AdminExistente_Retorna204()
    {
        using var auth = _factory.CreateClientWithTestAuth(roles: ["Admin"]);
        var (clientId, sellerId, productId) = await SeedCatalogAsync(auth);
        var orderId = await CreatePendingOrderIdAsync(auth, clientId, sellerId, productId);

        var response = await auth.Client.DeleteAsync($"/api/orders/{orderId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task PutOrder_IdMismatch_Retorna400Message()
    {
        using var auth = _factory.CreateClientWithTestAuth();

        var response = await auth.Client.PutAsJsonAsync("/api/orders/1", new
        {
            id = 2,
            clientId = 1,
            sellerId = (int?)null,
            productId = (int?)null,
            productName = "Producto Contrato",
            cantidad = 1,
            precioUnitario = 10.50m,
            estado = "Pendiente"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var doc = await ReadJsonAsync(response);
        Assert.Equal("El ID de la ruta no coincide con el cuerpo.", GetProperty(doc.RootElement, "message").GetString());
    }

    [Fact]
    public async Task PutOrder_Inexistente_Retorna404Message()
    {
        using var auth = _factory.CreateClientWithTestAuth();
        var (clientId, sellerId, productId) = await SeedCatalogAsync(auth);
        var missingOrderId = await auth.QueryDbAsync(async db =>
            (await db.Orders.Select(o => o.Id).ToListAsync()).DefaultIfEmpty(0).Max() + 1);

        var response = await auth.Client.PutAsJsonAsync($"/api/orders/{missingOrderId}", new
        {
            id = missingOrderId,
            clientId,
            sellerId,
            productId,
            productName = "Producto Contrato",
            cantidad = 1,
            precioUnitario = 10.50m,
            estado = "Pendiente"
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        using var doc = await ReadJsonAsync(response);
        Assert.Equal("Pedido no encontrado", GetProperty(doc.RootElement, "message").GetString());
    }

    [Fact]
    public async Task GetOrderById_Existente_Retorna200()
    {
        using var auth = _factory.CreateClientWithTestAuth();
        var (clientId, sellerId, productId) = await SeedCatalogAsync(auth);
        var orderId = await CreatePendingOrderIdAsync(auth, clientId, sellerId, productId);

        var response = await auth.Client.GetAsync($"/api/orders/{orderId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = await ReadJsonAsync(response);
        Assert.Equal(orderId, GetProperty(doc.RootElement, "id").GetInt32());
        Assert.Equal(clientId, GetProperty(doc.RootElement, "clientId").GetInt32());
    }

    [Fact]
    public async Task PutOrder_EditLocked_SoloPermiteCambioEstado()
    {
        using var auth = _factory.CreateClientWithTestAuth();
        var (clientId, sellerId, productId) = await SeedCatalogAsync(auth, productStock: 5);
        var orderId = await CreatePendingOrderIdAsync(auth, clientId, sellerId, productId);

        var processed = await auth.Client.PatchAsJsonAsync(
            $"/api/orders/{orderId}/estado", new { estado = "Procesado" });
        Assert.Equal(HttpStatusCode.NoContent, processed.StatusCode);

        // En estado Procesado el controller ignora los campos editables y solo
        // delega en UpdateStatusAsync: el cambio de cantidad no debe aplicarse.
        var response = await auth.Client.PutAsJsonAsync($"/api/orders/{orderId}", new
        {
            id = orderId,
            clientId,
            sellerId,
            productId,
            productName = "Producto Contrato",
            cantidad = 999,
            precioUnitario = 10.50m,
            estado = "Enviado"
        });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var persisted = await auth.QueryDbAsync(async db =>
        {
            var order = await db.Orders.FindAsync(orderId);
            return (Cantidad: order!.Cantidad, Estado: order.Estado);
        });
        Assert.Equal(1, persisted.Cantidad);
        Assert.Equal("Enviado", persisted.Estado);

        // El mismo flujo con un estado inválido viaja por UpdateStatusAsync.
        var invalidStatus = await auth.Client.PutAsJsonAsync($"/api/orders/{orderId}", new
        {
            id = orderId,
            clientId,
            sellerId,
            productId,
            productName = "Producto Contrato",
            cantidad = 1,
            precioUnitario = 10.50m,
            estado = "Inexistente"
        });

        Assert.Equal(HttpStatusCode.BadRequest, invalidStatus.StatusCode);
        using var doc = await ReadJsonAsync(invalidStatus);
        Assert.Equal("Estado no válido", GetProperty(doc.RootElement, "message").GetString());
    }

    [Fact]
    public async Task PutOrder_PendienteValido_Retorna204()
    {
        using var auth = _factory.CreateClientWithTestAuth();
        var (clientId, sellerId, productId) = await SeedCatalogAsync(auth, productStock: 5);
        var orderId = await CreatePendingOrderIdAsync(auth, clientId, sellerId, productId);

        // Pendiente no está en EditLocked: UpdateAsync aplica los campos editables.
        var response = await auth.Client.PutAsJsonAsync($"/api/orders/{orderId}", new
        {
            id = orderId,
            clientId,
            sellerId,
            productId,
            productName = "Producto Contrato",
            cantidad = 2,
            precioUnitario = 10.50m,
            estado = "Pendiente"
        });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var persisted = await auth.QueryDbAsync(async db =>
        {
            var order = await db.Orders.FindAsync(orderId);
            return (Cantidad: order!.Cantidad, Estado: order.Estado, Total: order.Total);
        });
        Assert.Equal(2, persisted.Cantidad);
        Assert.Equal("Pendiente", persisted.Estado);
        Assert.Equal(21.00m, persisted.Total);
    }

    [Fact]
    public async Task PutOrder_PendienteConClienteInexistente_Retorna400Message()
    {
        using var auth = _factory.CreateClientWithTestAuth();
        var (clientId, sellerId, productId) = await SeedCatalogAsync(auth);
        var orderId = await CreatePendingOrderIdAsync(auth, clientId, sellerId, productId);
        var missingClientId = await auth.QueryDbAsync(async db =>
            (await db.Clients.Select(c => c.Id).ToListAsync()).DefaultIfEmpty(0).Max() + 1);

        var response = await auth.Client.PutAsJsonAsync($"/api/orders/{orderId}", new
        {
            id = orderId,
            clientId = missingClientId,
            sellerId,
            productId,
            productName = "Producto Contrato",
            cantidad = 1,
            precioUnitario = 10.50m,
            estado = "Pendiente"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var doc = await ReadJsonAsync(response);
        Assert.Equal("Cliente no válido", GetProperty(doc.RootElement, "message").GetString());

        var persistedClientId = await auth.QueryDbAsync(async db =>
            (await db.Orders.FindAsync(orderId))!.ClientId);
        Assert.Equal(clientId, persistedClientId);
    }

    [Fact]
    public async Task PutOrder_PendienteConVendedorInexistente_Retorna400Message()
    {
        using var auth = _factory.CreateClientWithTestAuth();
        var (clientId, sellerId, productId) = await SeedCatalogAsync(auth);
        var orderId = await CreatePendingOrderIdAsync(auth, clientId, sellerId, productId);
        var missingSellerId = await auth.QueryDbAsync(async db =>
            (await db.Sellers.Select(s => s.Id).ToListAsync()).DefaultIfEmpty(0).Max() + 1);

        var response = await auth.Client.PutAsJsonAsync($"/api/orders/{orderId}", new
        {
            id = orderId,
            clientId,
            sellerId = missingSellerId,
            productId,
            productName = "Producto Contrato",
            cantidad = 1,
            precioUnitario = 10.50m,
            estado = "Pendiente"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var doc = await ReadJsonAsync(response);
        Assert.Equal("Vendedor no válido", GetProperty(doc.RootElement, "message").GetString());

        var persistedSellerId = await auth.QueryDbAsync(async db =>
            (await db.Orders.FindAsync(orderId))!.SellerId);
        Assert.Equal(sellerId, persistedSellerId);
    }

    [Fact]
    public async Task PutOrder_PendienteConVendedorInactivo_Retorna400Message()
    {
        using var auth = _factory.CreateClientWithTestAuth();
        var (clientId, sellerId, productId) = await SeedCatalogAsync(auth);
        var inactiveSellerId = await SeedSellerAsync(auth, active: false);
        var orderId = await CreatePendingOrderIdAsync(auth, clientId, sellerId, productId);

        var response = await auth.Client.PutAsJsonAsync($"/api/orders/{orderId}", new
        {
            id = orderId,
            clientId,
            sellerId = inactiveSellerId,
            productId,
            productName = "Producto Contrato",
            cantidad = 1,
            precioUnitario = 10.50m,
            estado = "Pendiente"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var doc = await ReadJsonAsync(response);
        Assert.Equal("El vendedor está inactivo.", GetProperty(doc.RootElement, "message").GetString());

        var persistedSellerId = await auth.QueryDbAsync(async db =>
            (await db.Orders.FindAsync(orderId))!.SellerId);
        Assert.Equal(sellerId, persistedSellerId);
    }

    [Fact]
    public async Task PatchOrderStatus_ProcesadoAEnviado_Retorna204()
    {
        using var auth = _factory.CreateClientWithTestAuth();
        var (clientId, sellerId, productId) = await SeedCatalogAsync(auth, productStock: 5);
        var orderId = await CreatePendingOrderIdAsync(auth, clientId, sellerId, productId);

        var procesado = await PatchStatusAsync(auth.Client, orderId, "Procesado");
        Assert.Equal(HttpStatusCode.NoContent, procesado.StatusCode);

        var response = await PatchStatusAsync(auth.Client, orderId, "Enviado");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var estado = await auth.QueryDbAsync(async db => (await db.Orders.FindAsync(orderId))!.Estado);
        Assert.Equal("Enviado", estado);
    }

    [Fact]
    public async Task PatchOrderStatus_EnviadoAEntregado_Retorna204()
    {
        using var auth = _factory.CreateClientWithTestAuth();
        var (clientId, sellerId, productId) = await SeedCatalogAsync(auth, productStock: 5);
        var orderId = await CreatePendingOrderIdAsync(auth, clientId, sellerId, productId);

        Assert.Equal(HttpStatusCode.NoContent, (await PatchStatusAsync(auth.Client, orderId, "Procesado")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await PatchStatusAsync(auth.Client, orderId, "Enviado")).StatusCode);

        var response = await PatchStatusAsync(auth.Client, orderId, "Entregado");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var estado = await auth.QueryDbAsync(async db => (await db.Orders.FindAsync(orderId))!.Estado);
        Assert.Equal("Entregado", estado);
    }

    [Fact]
    public async Task PatchOrderStatus_ProcesadoACancelado_Retorna204()
    {
        using var auth = _factory.CreateClientWithTestAuth();
        var (clientId, sellerId, productId) = await SeedCatalogAsync(auth, productStock: 5);
        var orderId = await CreatePendingOrderIdAsync(auth, clientId, sellerId, productId, cantidad: 2);

        Assert.Equal(HttpStatusCode.NoContent, (await PatchStatusAsync(auth.Client, orderId, "Procesado")).StatusCode);

        var response = await PatchStatusAsync(auth.Client, orderId, "Cancelado");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var persisted = await auth.QueryDbAsync(async db =>
        {
            var order = await db.Orders.FindAsync(orderId);
            var product = await db.Products.FindAsync(productId);
            return (Estado: order!.Estado, Stock: product!.Stock);
        });
        Assert.Equal("Cancelado", persisted.Estado);
        // El reembolso devuelve las 2 unidades descontadas al procesar.
        Assert.Equal(5, persisted.Stock);
    }

    [Fact]
    public async Task GetOrders_ConAuth_Retorna200Array()
    {
        using var auth = _factory.CreateClientWithTestAuth();
        var (clientId, sellerId, productId) = await SeedCatalogAsync(auth);
        var orderId = await CreatePendingOrderIdAsync(auth, clientId, sellerId, productId);

        var response = await auth.Client.GetAsync("/api/orders");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = await ReadJsonAsync(response);
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
        Assert.Contains(doc.RootElement.EnumerateArray(), o => GetProperty(o, "id").GetInt32() == orderId);
    }

    [Fact]
    public async Task PostOrder_ProductIdInexistente_Retorna400Message()
    {
        using var auth = _factory.CreateClientWithTestAuth();
        var (clientId, sellerId, _) = await SeedCatalogAsync(auth);
        var missingProductId = await auth.QueryDbAsync(async db =>
            (await db.Products.Select(p => p.Id).ToListAsync()).DefaultIfEmpty(0).Max() + 1);

        var response = await PostOrderAsync(auth.Client, clientId, sellerId, missingProductId);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var doc = await ReadJsonAsync(response);
        Assert.Equal("Producto no válido", GetProperty(doc.RootElement, "message").GetString());
    }

    [Fact]
    public async Task PostOrder_VendedorInexistente_Retorna400Message()
    {
        using var auth = _factory.CreateClientWithTestAuth();
        var clientId = await SeedClientAsync(auth);
        var productId = await SeedProductAsync(auth);
        var missingSellerId = await auth.QueryDbAsync(async db =>
            (await db.Sellers.Select(s => s.Id).ToListAsync()).DefaultIfEmpty(0).Max() + 1);

        var response = await PostOrderAsync(auth.Client, clientId, missingSellerId, productId);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var doc = await ReadJsonAsync(response);
        Assert.Equal("Vendedor no válido", GetProperty(doc.RootElement, "message").GetString());
    }

    [Fact]
    public async Task GetOrderStatuses_ConAuth_Retorna200Array()
    {
        using var auth = _factory.CreateClientWithTestAuth();

        var response = await auth.Client.GetAsync("/api/orders/statuses");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = await ReadJsonAsync(response);
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
        Assert.Contains("Pendiente", doc.RootElement.EnumerateArray().Select(s => s.GetString()).ToList());
    }

    private static Task<HttpResponseMessage> PatchStatusAsync(HttpClient client, int orderId, string estado) =>
        client.PatchAsJsonAsync($"/api/orders/{orderId}/estado", new { estado });

    private static async Task<(int ClientId, int SellerId, int ProductId)> SeedCatalogAsync(
        TestAuthClient auth, int productStock = 5)
    {
        var clientId = await SeedClientAsync(auth);
        var sellerId = await SeedSellerAsync(auth);
        var productId = await SeedProductAsync(auth, productStock);
        return (clientId, sellerId, productId);
    }

    private static Task<int> SeedClientAsync(TestAuthClient auth)
    {
        var suffix = Guid.NewGuid().ToString("N");
        return auth.QueryDbAsync(async db =>
        {
            var client = Client.Create($"Cliente Contrato {suffix}", $"orders.contract.client.{suffix}@test.local");
            db.Clients.Add(client);
            await db.SaveChangesAsync();
            return client.Id;
        });
    }

    private static Task<int> SeedSellerAsync(TestAuthClient auth, bool active = true)
    {
        var suffix = Guid.NewGuid().ToString("N");
        return auth.QueryDbAsync(async db =>
        {
            var seller = Seller.Create(
                $"Vendedor Contrato {suffix}",
                $"orders.contract.seller.{suffix}@test.local",
                active: active);
            db.Sellers.Add(seller);
            await db.SaveChangesAsync();
            return seller.Id;
        });
    }

    private static Task<int> SeedProductAsync(TestAuthClient auth, int stock = 5)
    {
        var suffix = Guid.NewGuid().ToString("N");
        return auth.QueryDbAsync(async db =>
        {
            var product = Product.Create(
                $"SKU-ORDERS-CONTRACT-{suffix}",
                $"Producto Contrato {suffix}",
                "General",
                10.50m,
                5.25m,
                stock,
                1);
            db.Products.Add(product);
            await db.SaveChangesAsync();
            return product.Id;
        });
    }

    private static Task<HttpResponseMessage> PostOrderAsync(
        HttpClient client, int clientId, int? sellerId, int? productId,
        int cantidad = 1, string? orderNumber = null) =>
        client.PostAsJsonAsync("/api/orders", new
        {
            clientId,
            sellerId,
            productId,
            productName = "Producto Contrato",
            cantidad,
            precioUnitario = 10.50m,
            orderNumber
        });

    private static async Task<int> CreatePendingOrderIdAsync(
        TestAuthClient auth, int clientId, int? sellerId, int? productId,
        int cantidad = 1, string? orderNumber = null)
    {
        var response = await PostOrderAsync(auth.Client, clientId, sellerId, productId, cantidad, orderNumber);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var doc = await ReadJsonAsync(response);
        return GetProperty(doc.RootElement, "id").GetInt32();
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
