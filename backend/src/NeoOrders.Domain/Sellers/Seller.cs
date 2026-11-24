using NeoOrders.Domain.Orders;

namespace NeoOrders.Domain.Sellers;

// KAN-38: definición persistible del vendedor. Sólo propiedades y navegaciones;
// el comportamiento vive en Seller.Behavior.cs. Sin dependencias de EF/AspNet/Identity/DataAnnotations.
// El esquema se configura en Infrastructure/Data/Configurations/SellerConfiguration.
// Los setters son privados: la creación/reconstrucción se hace con las factories
// Create/Rehydrate y las mutaciones con los métodos del dominio.
public partial class Seller
{
    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string Phone { get; private set; } = string.Empty;
    public decimal CommissionPercentage { get; private set; } = 5.0m;
    public bool Active { get; private set; } = true;

    // Navegación requerida por el mapeo EF (OrderConfiguration).
    public ICollection<Order> Orders { get; private set; } = new List<Order>();

    // Constructor para la materialización de EF; el resto del código usa las factories.
    private Seller() { }
}
