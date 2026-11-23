namespace NeoOrders.Application.DTOs.Orders;

public class OrderListItemDto
{
    public int Id { get; set; }
    public int ClientId { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public string ClientEmail { get; set; } = string.Empty;
    public int? SellerId { get; set; }
    public string SellerName { get; set; } = string.Empty;
    public int? ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Total { get; set; }
    public DateTime Fecha { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string? OrderNumber { get; set; }
}
