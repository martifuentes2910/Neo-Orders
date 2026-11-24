using NeoOrders.Application.DTOs.Orders;

namespace NeoOrders.Application.Contracts.Orders;

public interface IOrderQueryService
{
    /// <summary>
    /// Listado de pedidos. Los filtros se combinan con AND.
    /// <paramref name="productId"/> es un id exacto de producto.
    /// <paramref name="desde"/>/<paramref name="hasta"/> aceptan fechas del tipo
    /// yyyy-MM-dd (día completo inclusive) o timestamps ISO (inclusivos tal cual).
    /// </summary>
    Task<List<OrderListItemDto>> ListAsync(
        string? search,
        string? estado,
        int? clientId,
        int? sellerId,
        int? productId = null,
        string? desde = null,
        string? hasta = null);
    Task<OrderListItemDto?> GetByIdAsync(int id);
    Task<string?> GetEstadoAsync(int id);
}
