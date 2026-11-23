using NeoOrders.Application.Contracts.Orders;
using NeoOrders.Application.Contracts.Notifications;
using NeoOrders.Application.Contracts.Import;
using Microsoft.EntityFrameworkCore;
using NeoOrders.Infrastructure.Data;
using NeoOrders.Infrastructure.Models;
using NeoOrders.Domain.Orders;

namespace NeoOrders.Infrastructure.Services;

public class OrderService : IOrderService
{
    private readonly AppDbContext _db;
    public OrderService(AppDbContext db) => _db = db;

    public async Task<OrderResult> CreateAsync(int clientId, int? sellerId, int? productId, int cantidad, string? orderNumber = null)
    {
        if (!await _db.Clients.AnyAsync(c => c.Id == clientId))
            return new OrderResult { Success = false, Error = "Cliente no v├ílido" };
        if (sellerId.HasValue && !await _db.Sellers.AnyAsync(s => s.Id == sellerId.Value))
            return new OrderResult { Success = false, Error = "Vendedor no v├ílido" };
        if (cantidad < 1)
            return new OrderResult { Success = false, Error = "Cantidad inv├ílida" };
        if (!productId.HasValue)
            return new OrderResult { Success = false, Error = "Debe seleccionar un producto del cat├ílogo" };

        var normalizedOrderNumber = string.IsNullOrWhiteSpace(orderNumber) ? null : orderNumber.Trim();
        if (normalizedOrderNumber != null)
        {
            if (normalizedOrderNumber.Length > 50)
                return new OrderResult { Success = false, Error = "OrderNumber no puede superar 50 caracteres" };
            if (await _db.Orders.AnyAsync(o => o.OrderNumber == normalizedOrderNumber))
                return new OrderResult { Success = false, Error = "OrderNumber ya existe" };
        }

        // Nombre y precio son del cat├ílogo, nunca del payload del cliente (KAN-13).
        var prod = await _db.Products.FindAsync(productId.Value);
        if (prod == null) return new OrderResult { Success = false, Error = "Producto no v├ílido" };

        // KAN-35/KAN-38: Única rutina de transición. Aplica efectos de stock y cambia estado.
    // No llama SaveChanges; el caller controla transacción/persistencia.
    private async Task<OrderResult> ApplyStatusTransitionAsync(Order order, string newStatusRaw)
    {
        if (!OrderStatusMapper.TryParse(newStatusRaw, out var newStatus))
            return new OrderResult { Success = false, Error = "Estado no válido" };
        if (!OrderStatusMapper.TryParse(order.Estado, out var currentStatus))
            return new OrderResult { Success = false, Error = "Estado actual no válido" };

        var newStatusValue = newStatus.ToExternal();
        if (!OrderStatuses.CanTransition(currentStatus, newStatus))
            return new OrderResult { Success = false, Error = $"Transición de estado no permitida: {order.Estado} → {newStatusValue}" };

        if (currentStatus == newStatus)
            return new OrderResult { Success = true, OrderId = order.Id, ProductName = order.ProductName };

        if (currentStatus == OrderStatus.Pendiente && newStatus == OrderStatus.Procesado)
        {
            if (!order.ProductId.HasValue)
                return new OrderResult { Success = false, Error = "No se puede procesar: el pedido no tiene un producto de catálogo asociado" };

            var product = await GetProductForStockUpdateAsync(order.ProductId.Value);
            if (product == null)
                return new OrderResult { Success = false, Error = "No se puede procesar: el producto del pedido ya no existe en el catálogo" };
            if (product.Stock < order.Cantidad)
                return new OrderResult { Success = false, Error = "Stock insuficiente" };

            product.Stock -= order.Cantidad;
        }
        else if (currentStatus == OrderStatus.Procesado && newStatus == OrderStatus.Cancelado)
        {
            if (!order.ProductId.HasValue)
                return new OrderResult { Success = false, Error = "No se puede cancelar: el pedido no tiene un producto de catálogo asociado para reembolsar stock" };

            var product = await GetProductForStockUpdateAsync(order.ProductId.Value);
            if (product == null)
                return new OrderResult { Success = false, Error = "No se puede cancelar: el producto del pedido ya no existe en el catálogo para reembolsar stock" };

            product.Stock += order.Cantidad;
        }

        order.Estado = newStatusValue;
        return new OrderResult { Success = true, OrderId = order.Id, ProductName = order.ProductName };
    }

    private async Task<Product?> GetProductForStockUpdateAsync(int productId)
    {
        // KAN-35: bloqueo de fila para evitar oversell/reembolsos concurrentes inconsistentes.
        if (_db.Database.IsRelational())
        {
            return await _db.Products
                .FromSqlRaw("SELECT * FROM Products WITH (UPDLOCK, ROWLOCK) WHERE Id = {0}", productId)
                .FirstOrDefaultAsync();
        }
        return await _db.Products.FirstOrDefaultAsync(p => p.Id == productId);
    }
}
