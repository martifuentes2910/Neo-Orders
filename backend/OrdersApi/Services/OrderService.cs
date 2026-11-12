using Microsoft.EntityFrameworkCore;
using OrdersApi.Data;
using OrdersApi.Models;

namespace OrdersApi.Services;

public class OrderService : IOrderService
{
    private readonly AppDbContext _db;
    public OrderService(AppDbContext db) => _db = db;

    public async Task<OrderResult> CreateAsync(int clientId, int? sellerId, int? productId, string productName, int cantidad, decimal precioUnitario)
    {
        if (!await _db.Clients.AnyAsync(c => c.Id == clientId))
            return new OrderResult { Success = false, Error = "Cliente no válido" };
        if (sellerId.HasValue && !await _db.Sellers.AnyAsync(s => s.Id == sellerId.Value))
            return new OrderResult { Success = false, Error = "Vendedor no válido" };
        if (productId.HasValue)
        {
            var prod = await _db.Products.FindAsync(productId.Value);
            if (prod == null) return new OrderResult { Success = false, Error = "Producto no válido" };
            if (prod.Stock < cantidad) return new OrderResult { Success = false, Error = "Stock insuficiente" };
            prod.Stock -= cantidad;
        }

        var order = new Order
        {
            ClientId = clientId,
            SellerId = sellerId,
            ProductId = productId,
            ProductName = productName,
            Cantidad = cantidad,
            PrecioUnitario = precioUnitario,
            Total = cantidad * precioUnitario,
            Fecha = DateTime.UtcNow,
            Estado = OrderStatuses.Pendiente
        };
        _db.Orders.Add(order);
        await _db.SaveChangesAsync();
        return new OrderResult { Success = true, OrderId = order.Id };
    }

    public async Task<bool> UpdateAsync(int id, int clientId, int? sellerId, int? productId, string productName, int cantidad, decimal precioUnitario, string estado)
    {
        var existing = await _db.Orders.FindAsync(id);
        if (existing == null) return false;
        if (!await _db.Clients.AnyAsync(c => c.Id == clientId)) return false;
        existing.ClientId = clientId;
        existing.SellerId = sellerId;
        existing.ProductId = productId;
        existing.ProductName = productName;
        existing.Cantidad = cantidad;
        existing.PrecioUnitario = precioUnitario;
        existing.Total = cantidad * precioUnitario;
        existing.Estado = estado;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var p = await _db.Orders.FindAsync(id);
        if (p == null) return false;
        _db.Orders.Remove(p);
        await _db.SaveChangesAsync();
        return true;
    }
}
