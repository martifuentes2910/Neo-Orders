namespace NeoOrders.Application.Contracts.Orders;

public interface IOrderService
{
    Task<OrderResult> CreateAsync(int clientId, int? sellerId, int? productId, int cantidad, string? orderNumber = null);
    Task<OrderResult> UpdateAsync(int id, int clientId, int? sellerId, int cantidad, string estado);
    Task<OrderResult> UpdateStatusAsync(int id, string estado);
    Task<bool> DeleteAsync(int id);
}
