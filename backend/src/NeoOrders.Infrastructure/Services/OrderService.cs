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
            return new OrderResult { Success = false, Error = "Cliente no válido" };
        if (sellerId.HasValue && !await _db.Sellers.AnyAsync(s => s.Id == sellerId.Value))
            return new OrderResult { Success = false, Error = "Vendedor no válido" };
        if (cantidad < 1)
            return new OrderResult { Success = false, Error = "Cantidad inválida" };
        if (!productId.HasValue)
            return new OrderResult { Success = false, Error = "Debe seleccionar un producto del catálogo" };

        var normalizedOrderNumber = string.IsNullOrWhiteSpace(orderNumber) ? null : orderNumber.Trim();
        if (normalizedOrderNumber != null)
        {
            if (normalizedOrderNumber.Length > 50)
                return new OrderResult { Success = false, Error = "OrderNumber no puede superar 50 caracteres" };
            if (await _db.Orders.AnyAsync(o => o.OrderNumber == normalizedOrderNumber))
                return new OrderResult { Success = false, Error = "OrderNumber ya existe" };
        }

        // Nombre y precio son del catálogo, nunca del payload del cliente (KAN-13).
        var prod = await _db.Products.FindAsync(productId.Value);
        if (prod == null) return new OrderResult { Success = false, Error = "Producto no válido" };

        // KAN-35: el stock se descuenta al pasar a Procesado, no al crear el pedido.
        var order = new Order
        {
            OrderNumber = normalizedOrderNumber,
            ClientId = clientId,
            SellerId = sellerId,
            ProductId = productId,
            ProductName = prod.Name,
            Cantidad = cantidad,
            PrecioUnitario = prod.BasePrice,
            Total = cantidad * prod.BasePrice,
            Fecha = DateTime.UtcNow,
            Estado = OrderStatuses.Pendiente
        };
        _db.Orders.Add(order);
        await _db.SaveChangesAsync();
        return new OrderResult { Success = true, OrderId = order.Id, ProductName = order.ProductName };
    }

    public async Task<OrderResult> UpdateAsync(int id, int clientId, int? sellerId, int cantidad, string estado)
    {
        var existing = await _db.Orders.FindAsync(id);
        if (existing == null)
            return new OrderResult { Success = false, NotFound = true, Error = "Pedido no encontrado" };
        // KAN-16/KAN-35: pedidos Procesado/Enviado/Entregado/Cancelado no se editan; sólo cambia su estado.
        if (OrderStatuses.EditLocked.Contains(existing.Estado))
            return new OrderResult
            {
                Success = false,
                Error = $"El pedido está {existing.Estado}: no se puede editar, sólo cambiar el estado"
            };
        if (!await _db.Clients.AnyAsync(c => c.Id == clientId))
            return new OrderResult { Success = false, Error = "Cliente no válido" };
        if (sellerId.HasValue && !await _db.Sellers.AnyAsync(s => s.Id == sellerId.Value))
            return new OrderResult { Success = false, Error = "Vendedor no válido" };
        if (cantidad < 1)
            return new OrderResult { Success = false, Error = "Cantidad inválida" };
        if (!OrderStatuses.All.Contains(estado))
            return new OrderResult { Success = false, Error = "Estado no válido" };
        if (!OrderStatuses.CanTransition(existing.Estado, estado))
            return new OrderResult
            {
                Success = false,
                Error = $"Transición de estado no permitida: {existing.Estado} → {estado}"
            };

        var tx = _db.Database.IsRelational() ? await _db.Database.BeginTransactionAsync() : null;
        try
        {
            // KAN-13: identidad del producto y precio histórico intactos.
            existing.ClientId = clientId;
            existing.SellerId = sellerId;
            existing.Cantidad = cantidad;
            existing.Total = cantidad * existing.PrecioUnitario;

            var transition = await ApplyStatusTransitionAsync(existing, estado);
            if (!transition.Success)
            {
                if (tx != null) await tx.RollbackAsync();
                return transition;
            }

            await _db.SaveChangesAsync();
            if (tx != null) await tx.CommitAsync();
            return new OrderResult { Success = true, OrderId = id, ProductName = existing.ProductName };
        }
        catch
        {
            if (tx != null) await tx.RollbackAsync();
            throw;
        }
        finally
        {
            if (tx != null) await tx.DisposeAsync();
        }
    }

    public async Task<OrderResult> UpdateStatusAsync(int id, string estado)
    {
        var existing = await _db.Orders.FindAsync(id);
        if (existing == null)
            return new OrderResult { Success = false, NotFound = true, Error = "Pedido no encontrado" };
        if (!OrderStatuses.All.Contains(estado))
            return new OrderResult { Success = false, Error = "Estado no válido" };
        if (!OrderStatuses.CanTransition(existing.Estado, estado))
            return new OrderResult
            {
                Success = false,
                Error = $"Transición de estado no permitida: {existing.Estado} → {estado}"
            };

        var tx = _db.Database.IsRelational() ? await _db.Database.BeginTransactionAsync() : null;
        try
        {
            var transition = await ApplyStatusTransitionAsync(existing, estado);
            if (!transition.Success)
            {
                if (tx != null) await tx.RollbackAsync();
                return transition;
            }

            await _db.SaveChangesAsync();
            if (tx != null) await tx.CommitAsync();
            return new OrderResult { Success = true, OrderId = id, ProductName = existing.ProductName };
        }
        catch
        {
            if (tx != null) await tx.RollbackAsync();
            throw;
        }
        finally
        {
            if (tx != null) await tx.DisposeAsync();
        }
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var p = await _db.Orders.FindAsync(id);
        if (p == null) return false;
        _db.Orders.Remove(p);
        await _db.SaveChangesAsync();
        return true;
    }

    // KAN-35: única rutina de transición. Aplica efectos de stock y cambia estado.
    // No llama SaveChanges; el caller controla transacción/persistencia.
    private async Task<OrderResult> ApplyStatusTransitionAsync(Order order, string newStatus)
    {
        if (order.Estado == newStatus)
        {
            order.Estado = newStatus;
            return new OrderResult { Success = true, OrderId = order.Id, ProductName = order.ProductName };
        }

        if (order.Estado == OrderStatuses.Pendiente && newStatus == OrderStatuses.Procesado)
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
        else if (order.Estado == OrderStatuses.Procesado && newStatus == OrderStatuses.Cancelado)
        {
            if (!order.ProductId.HasValue)
                return new OrderResult { Success = false, Error = "No se puede cancelar: el pedido no tiene un producto de catálogo asociado para reembolsar stock" };

            var product = await GetProductForStockUpdateAsync(order.ProductId.Value);
            if (product == null)
                return new OrderResult { Success = false, Error = "No se puede cancelar: el producto del pedido ya no existe en el catálogo para reembolsar stock" };

            product.Stock += order.Cantidad;
        }

        order.Estado = newStatus;
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
