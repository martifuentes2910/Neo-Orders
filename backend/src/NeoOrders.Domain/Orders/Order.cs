using NeoOrders.Domain.Clients;
using NeoOrders.Domain.Products;
using NeoOrders.Domain.Sellers;

namespace NeoOrders.Domain.Orders;

// KAN-38: modelo de dominio del pedido, también tipo persistible mapeado por EF
// mediante OrderConfiguration en Infrastructure. Sin dependencias de EF/AspNet/Identity.
// Definición del modelo; comportamiento en Order.Behavior.cs.
public sealed partial class Order
{
    public int Id { get; set; }
    public int ClientId { get; set; }
    public int? SellerId { get; set; }
    public int? ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Total { get; set; }
    public DateTime Fecha { get; set; }
    public string Estado { get; set; } = OrderStatuses.Pendiente;
    public string? OrderNumber { get; set; }

    // Navegaciones requeridas por el mapeo EF (configuradas en OrderConfiguration).
    public Client? Client { get; set; }
    public Seller? Seller { get; set; }
    public Product? Product { get; set; }

    public OrderStatus Status => OrderStatusMapper.FromExternal(Estado);

    public bool IsEditLocked => OrderStatuses.EditLocked.Contains(Estado);
}
