namespace OrdersApi.Services;

public interface IOrderService
{
    Task<OrderResult> CreateAsync(int clientId, int? sellerId, int? productId, string productName, int cantidad, decimal precioUnitario);
    Task<bool> UpdateAsync(int id, int clientId, int? sellerId, int? productId, string productName, int cantidad, decimal precioUnitario, string estado);
    Task<bool> DeleteAsync(int id);
}

public class OrderResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public int? OrderId { get; set; }
}
