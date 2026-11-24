using NeoOrders.Domain.Orders;

namespace NeoOrders.Domain.Clients;

// KAN-38: definición persistible del cliente. Sólo propiedades y navegaciones;
// el comportamiento vive en Client.Behavior.cs. Sin dependencias de EF/AspNet/Identity/DataAnnotations.
// El esquema se configura en Infrastructure/Data/Configurations/ClientConfiguration.
// Los setters son privados: la creación/reconstrucción se hace con las factories
// Create/Rehydrate y las mutaciones con los métodos del dominio.
public partial class Client
{
    public int Id { get; private set; }
    public string Nombre { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string? Telefono { get; private set; }
    public string? Direccion { get; private set; }
    public DateTime FechaAlta { get; private set; } = DateTime.UtcNow;

    // Navegación requerida por el mapeo EF (OrderConfiguration).
    public ICollection<Order> Orders { get; private set; } = new List<Order>();

    // Constructor para la materialización de EF; el resto del código usa las factories.
    private Client() { }
}
