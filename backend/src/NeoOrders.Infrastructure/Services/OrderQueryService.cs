using Microsoft.EntityFrameworkCore;
using NeoOrders.Application.Contracts.Orders;
using NeoOrders.Application.DTOs.Orders;
using NeoOrders.Infrastructure.Data;

namespace NeoOrders.Infrastructure.Services;

public class OrderQueryService : IOrderQueryService
{
    private readonly AppDbContext _db;
    public OrderQueryService(AppDbContext db) => _db = db;

    public async Task<List<OrderListItemDto>> ListAsync(string? search, string? estado, int? clientId, int? sellerId)
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
}
