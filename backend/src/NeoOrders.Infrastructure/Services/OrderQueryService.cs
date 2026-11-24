using System.Globalization;
using Microsoft.EntityFrameworkCore;
using NeoOrders.Application.Contracts.Orders;
using NeoOrders.Application.DTOs.Orders;
using NeoOrders.Infrastructure.Data;

namespace NeoOrders.Infrastructure.Services;

public class OrderQueryService : IOrderQueryService
{
    private readonly AppDbContext _db;
    public OrderQueryService(AppDbContext db) => _db = db;

    public async Task<List<OrderListItemDto>> ListAsync(
        string? search,
        string? estado,
        int? clientId,
        int? sellerId,
        int? productId = null,
        string? desde = null,
        string? hasta = null)
    {
        var q = _db.Orders.Include(p => p.Client).Include(p => p.Seller).AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            q = q.Where(p => p.ProductName.ToLower().Contains(s) ||
                             (p.Client != null && p.Client.Nombre.ToLower().Contains(s)));
        }
        if (!string.IsNullOrWhiteSpace(estado) && estado != "All" && estado != "Todos")
            q = q.Where(p => p.Estado == estado);
        if (clientId.HasValue) q = q.Where(p => p.ClientId == clientId.Value);
        if (sellerId.HasValue) q = q.Where(p => p.SellerId == sellerId.Value);
        if (productId.HasValue) q = q.Where(p => p.ProductId == productId.Value);

        var from = ParseDateBoundary(desde, endOfDay: false);
        if (from.HasValue) q = q.Where(p => p.Fecha >= from.Value);

        var to = ParseDateBoundary(hasta, endOfDay: true);
        if (to.HasValue) q = q.Where(p => p.Fecha <= to.Value);

        return await q.OrderByDescending(p => p.Fecha).Select(p => new OrderListItemDto
        {
            Id = p.Id,
            ClientId = p.ClientId,
            ClientName = p.Client != null ? p.Client.Nombre : "",
            ClientEmail = p.Client != null ? p.Client.Email : "",
            SellerId = p.SellerId,
            SellerName = p.Seller != null ? p.Seller.Name : "",
            ProductId = p.ProductId,
            ProductName = p.ProductName,
            Cantidad = p.Cantidad,
            PrecioUnitario = p.PrecioUnitario,
            Total = p.Total,
            Fecha = p.Fecha,
            Estado = p.Estado,
            OrderNumber = p.OrderNumber
        }).ToListAsync();
    }

    public async Task<OrderListItemDto?> GetByIdAsync(int id)
    {
        return await _db.Orders.Include(p => p.Client).Include(p => p.Seller).AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new OrderListItemDto
            {
                Id = p.Id,
                ClientId = p.ClientId,
                ClientName = p.Client != null ? p.Client.Nombre : "",
                ClientEmail = p.Client != null ? p.Client.Email : "",
                SellerId = p.SellerId,
                SellerName = p.Seller != null ? p.Seller.Name : "",
                ProductId = p.ProductId,
                ProductName = p.ProductName,
                Cantidad = p.Cantidad,
                PrecioUnitario = p.PrecioUnitario,
                Total = p.Total,
                Fecha = p.Fecha,
                Estado = p.Estado,
                OrderNumber = p.OrderNumber
            }).FirstOrDefaultAsync();
    }

    public Task<string?> GetEstadoAsync(int id) =>
        _db.Orders.AsNoTracking().Where(o => o.Id == id).Select(o => o.Estado).FirstOrDefaultAsync();

    /// <summary>
    /// Normaliza los límites de fecha del listado:
    /// - yyyy-MM-dd (lo que envía el &lt;input type="date"&gt; del FE): desde es el
    ///   inicio del día y hasta el último tick del día, ambos inclusivos, para que
    ///   elegir un solo día no excluya los pedidos de ese día.
    /// - timestamp ISO con/sin offset: se respeta tal cual (también inclusivo) y
    ///   se interpreta como UTC, igual que Order.Fecha.
    /// - texto no reconocido: se ignora el filtro en vez de romper el listado.
    /// </summary>
    private static DateTime? ParseDateBoundary(string? value, bool endOfDay)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var text = value.Trim();

        if (DateOnly.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var date))
            return date.ToDateTime(endOfDay ? TimeOnly.MaxValue : TimeOnly.MinValue, DateTimeKind.Utc);

        if (DateTime.TryParse(text, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var timestamp))
            return timestamp;

        return null;
    }
}
