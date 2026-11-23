using NeoOrders.Application.DTOs.Orders;

namespace NeoOrders.Application.Contracts.Orders;

public interface IOrderQueryService
{
    Task<List<OrderListItemDto>> ListAsync(string? search, string? estado, int? clientId, int? sellerId);
}
