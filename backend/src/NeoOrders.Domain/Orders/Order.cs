using NeoOrders.Domain.Clients;
using NeoOrders.Domain.Products;
using NeoOrders.Domain.Sellers;

namespace NeoOrders.Domain.Orders;

// KAN-38: definición persistible del pedido. Sólo propiedades y navegaciones;
// el comportamiento vive en Order.Behavior.cs. Sin dependencias de EF/AspNet/Identity.
// Los setters son privados: la creación/reconstrucción se hace con las factories
// CreateNew/Rehydrate y las mutaciones con los métodos del dominio.
public sealed partial class Order
{
    public int Id { get; private set; }
    public int ClientId { get; private set; }
    public int? SellerId { get; private set; }
    public int? ProductId { get; private set; }
    public string ProductName { get; private set; } = string.Empty;
    public int Cantidad { get; private set; }
    public decimal PrecioUnitario { get; private set; }
    public decimal Total { get; private set; }
    public DateTime Fecha { get; private set; }
    public string Estado { get; private set; } = OrderStatuses.Pendiente;
    public string? OrderNumber { get; private set; }

    // Navegaciones requeridas por el mapeo EF (configuradas en OrderConfiguration).
    public Client? Client { get; private set; }
    public Seller? Seller { get; private set; }
    public Product? Product { get; private set; }
}
