using NeoOrders.Domain.Orders;

namespace NeoOrders.Application.DTOs.Orders;

public class CreateOrderDto
{
    public int ClientId { get; set; }
    public int? SellerId { get; set; }
    public int? ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Cantidad { get; set; } = 1;
    public decimal PrecioUnitario { get; set; }
    public string? OrderNumber { get; set; }
}

public class UpdateOrderStatusDto
{
    public string Estado { get; set; } = string.Empty;
}

public class UpdateOrderDto
{
    public int Id { get; set; }
    public int ClientId { get; set; }
    public int? SellerId { get; set; }
    public int? ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Cantidad { get; set; } = 1;
    public decimal PrecioUnitario { get; set; }
    public string Estado { get; set; } = OrderStatusPolicy.Pendiente;
}
