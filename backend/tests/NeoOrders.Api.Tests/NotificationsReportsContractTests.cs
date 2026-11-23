using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using NeoOrders.Infrastructure.Models;

namespace NeoOrders.Api.Tests;

/// <summary>
/// Contratos HTTP aún no cubiertos de /api/notifications, /api/reports y del
/// bloque "recientes" de /api/dashboard/metrics (no existe un endpoint propio
/// /api/dashboard/recent-orders: los pedidos recientes viajan dentro de metrics).
/// Cada request con auth usa una factory derivada con DB InMemory aislada.
/// </summary>
public class NotificationsReportsContractTests : IClassFixture<NeoOrdersApiFactory>
{
    private readonly NeoOrdersApiFactory _factory;

    public NotificationsReportsContractTests(NeoOrdersApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetNotifications_ConAuth_Retorna200ConItemsYResumen()
    {
        using var auth = _factory.CreateClientWithTestAuth();
        await SeedNotificationContextAsync(auth);

        var response = await auth.Client.GetAsync("/api/notifications");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = await ReadJsonAsync(response);
        var root = doc.RootElement;
        Assert.Equal(JsonValueKind.Object, root.ValueKind);

        var items = GetProperty(root, "items");
        Assert.Equal(JsonValueKind.Array, items.ValueKind);
        Assert.Equal(2, items.GetArrayLength());
        var item = items[0];
        Assert.True(GetProperty(item, "id").GetInt32() > 0);
        Assert.False(string.IsNullOrWhiteSpace(GetProperty(item, "tipo").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(GetProperty(item, "accion").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(GetProperty(item, "titulo").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(GetProperty(item, "mensaje").GetString()));
        Assert.True(GetProperty(item, "fecha").TryGetDateTime(out _));
        Assert.True(GetProperty(item, "entidadId").TryGetInt32(out _));

        var resumen = GetProperty(root, "resumen");
        Assert.Equal(JsonValueKind.Object, resumen.ValueKind);
        Assert.Equal(2, GetProperty(resumen, "total").GetInt32());
        Assert.Equal(2, GetProperty(resumen, "hoy").GetInt32());
        Assert.Equal(1, GetProperty(resumen, "stockCritico").GetInt32());
        Assert.Equal(1, GetProperty(resumen, "pendientes").GetInt32());
    }

    [Fact]
    public async Task GetNotifications_ConTakeUno_RetornaUnItemYResumenDelPagina()
    {
        using var auth = _factory.CreateClientWithTestAuth();
        await SeedNotificationContextAsync(auth);

        var response = await auth.Client.GetAsync("/api/notifications?take=1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = await ReadJsonAsync(response);
        var root = doc.RootElement;
        var items = GetProperty(root, "items");
        Assert.Equal(1, items.GetArrayLength());
        // El contrato real calcula el resumen sobre la página devuelta por `take`.
        Assert.Equal(1, GetProperty(GetProperty(root, "resumen"), "total").GetInt32());
    }

    [Fact]
    public async Task GetReports_ConAuth_Retorna200ConShapeResumen()
    {
        using var auth = _factory.CreateClientWithTestAuth();

        var response = await auth.Client.GetAsync("/api/reports");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = await ReadJsonAsync(response);
        var root = doc.RootElement;
        Assert.Equal(JsonValueKind.Object, root.ValueKind);
        foreach (var name in new[] { "porEstado", "porMes", "topProductos", "topClientes", "ventasPorVendedor" })
        {
            Assert.Equal(JsonValueKind.Array, GetProperty(root, name).ValueKind);
        }
    }

    [Theory]
    [InlineData("/api/reports/ventas-vendedor", "vendedor")]
    [InlineData("/api/reports/productos-vendidos", "producto")]
    [InlineData("/api/reports/clientes-compras", "cliente")]
    public async Task GetReportesDetalle_ConAuth_Retorna200ConShapePaginado(string url, string tipo)
    {
        using var auth = _factory.CreateClientWithTestAuth();

        var response = await auth.Client.GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = await ReadJsonAsync(response);
        var root = doc.RootElement;
        Assert.Equal(tipo, GetProperty(root, "tipo").GetString());
        Assert.Equal(1, GetProperty(root, "pagina").GetInt32());
        Assert.Equal(25, GetProperty(root, "tamanoPagina").GetInt32());
        Assert.True(GetProperty(root, "totalFilas").TryGetInt32(out _));
        Assert.True(GetProperty(root, "totalPaginas").TryGetInt32(out _));
        Assert.Equal(JsonValueKind.Array, GetProperty(root, "filas").ValueKind);
        Assert.Equal(JsonValueKind.Array, GetProperty(root, "filasGrafico").ValueKind);
    }

    [Theory]
    [InlineData("/api/reports")]
    [InlineData("/api/reports/ventas-vendedor")]
    [InlineData("/api/reports/productos-vendidos")]
    [InlineData("/api/reports/clientes-compras")]
    public async Task GetReportes_SinAuth_Retorna401(string url)
    {
        using var client = CreateAnonymousClient();

        var response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/reports/productos-vendidos?page=0", "page")]
    [InlineData("/api/reports/clientes-compras?pageSize=101", "pageSize")]
    public async Task GetReportesDetalle_PaginacionInvalida_Retorna400ConMessage(string url, string campo)
    {
        using var auth = _factory.CreateClientWithTestAuth();

        var response = await auth.Client.GetAsync(url);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var doc = await ReadJsonAsync(response);
        Assert.Contains(campo, GetProperty(doc.RootElement, "message").GetString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetDashboardMetrics_ConAuth_RecientesIncluyePedidoSembrado()
    {
        using var auth = _factory.CreateClientWithTestAuth();
        var (orderId, clientName, productName) = await SeedOrderAsync(auth);

        var response = await auth.Client.GetAsync("/api/dashboard/metrics");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = await ReadJsonAsync(response);
        var root = doc.RootElement;
        Assert.Equal(1, GetProperty(root, "totalPedidos").GetInt32());
        Assert.Equal(1, GetProperty(root, "pendientes").GetInt32());
        var recientes = GetProperty(root, "recientes");
        Assert.Equal(JsonValueKind.Array, recientes.ValueKind);
        var reciente = Assert.Single(recientes.EnumerateArray());
        Assert.Equal(orderId, GetProperty(reciente, "id").GetInt32());
        Assert.Equal(clientName, GetProperty(reciente, "clientName").GetString());
        Assert.Equal(productName, GetProperty(reciente, "productName").GetString());
        Assert.Equal("Pendiente", GetProperty(reciente, "estado").GetString());
        Assert.True(GetProperty(reciente, "total").GetDecimal() > 0);
    }

    private HttpClient CreateAnonymousClient() =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    private static async Task SeedNotificationContextAsync(TestAuthClient auth)
    {
        var suffix = Guid.NewGuid().ToString("N");
        await auth.QueryDbAsync<object?>(async db =>
        {
            var now = DateTime.UtcNow;
            db.Notifications.AddRange(
                new Notification
                {
                    Tipo = "pedido",
                    Accion = "creado",
                    Titulo = $"Pedido {suffix}",
                    Mensaje = "Alta de pedido",
                    Usuario = "Sistema",
                    EntidadId = 1,
                    Fecha = now
                },
                new Notification
                {
                    Tipo = "cliente",
                    Accion = "creado",
                    Titulo = $"Cliente {suffix}",
                    Mensaje = "Alta de cliente",
                    Fecha = now
                });

            db.Products.Add(new Product
            {
                Sku = $"SKU-NOTIF-{suffix}",
                Name = $"Producto stock crítico {suffix}",
                Category = "General",
                BasePrice = 10.50m,
                Cost = 5.25m,
                Stock = 1,
                MinStockAlert = 2,
                Active = true
            });

            var client = new Client
            {
                Nombre = $"Cliente notificación {suffix}",
                Email = $"notif.client.{suffix}@test.local"
            };
            db.Clients.Add(client);
            await db.SaveChangesAsync();

            db.Orders.Add(new Order
            {
                ClientId = client.Id,
                ProductName = $"Pedido pendiente {suffix}",
                Cantidad = 1,
                PrecioUnitario = 10.50m,
                Total = 10.50m,
                Estado = "Pendiente",
                Fecha = now
            });
            await db.SaveChangesAsync();
            return null;
        });
    }

    private static async Task<(int OrderId, string ClientName, string ProductName)> SeedOrderAsync(TestAuthClient auth)
    {
        var suffix = Guid.NewGuid().ToString("N");
        var clientName = $"Cliente dashboard {suffix}";
        var productName = $"Producto dashboard {suffix}";
        return await auth.QueryDbAsync(async db =>
        {
            var client = new Client
            {
                Nombre = clientName,
                Email = $"dashboard.client.{suffix}@test.local"
            };
            var seller = new Seller
            {
                Name = $"Vendedor dashboard {suffix}",
                Email = $"dashboard.seller.{suffix}@test.local",
                Active = true
            };
            var product = new Product
            {
                Sku = $"SKU-DASH-{suffix}",
                Name = productName,
                Category = "General",
                BasePrice = 10.50m,
                Cost = 5.25m,
                Stock = 5,
                MinStockAlert = 1,
                Active = true
            };
            db.Clients.Add(client);
            db.Sellers.Add(seller);
            db.Products.Add(product);
            await db.SaveChangesAsync();

            var order = new Order
            {
                OrderNumber = $"DASH-{suffix}"[..20],
                ClientId = client.Id,
                SellerId = seller.Id,
                ProductId = product.Id,
                ProductName = productName,
                Cantidad = 1,
                PrecioUnitario = 10.50m,
                Total = 10.50m,
                Estado = "Pendiente",
                Fecha = DateTime.UtcNow
            };
            db.Orders.Add(order);
            await db.SaveChangesAsync();
            return (order.Id, clientName, productName);
        });
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
