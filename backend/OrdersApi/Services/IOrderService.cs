namespace OrdersApi.Services;

public interface IOrderService
{
    // El nombre y el precio se derivan del catálogo; el cliente no los elige.
    Task<OrderResult> CreateAsync(int clientId, int? sellerId, int? productId, int cantidad, string? orderNumber = null);
    // ProductId, ProductName y PrecioUnitario del pedido existente quedan intactos.
    Task<OrderResult> UpdateAsync(int id, int clientId, int? sellerId, int cantidad, string estado);
    // KAN-16: en pedidos finalizados/enviados se permite únicamente cambiar el estado.
    Task<OrderResult> UpdateStatusAsync(int id, string estado);
    Task<bool> DeleteAsync(int id);
}

public class OrderResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public int? OrderId { get; set; }
    public string? ProductName { get; set; }
    // true = el pedido no existe; false con Success = false = datos inválidos.
    public bool NotFound { get; set; }
}
