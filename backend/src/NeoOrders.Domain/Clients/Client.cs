using NeoOrders.Domain.Orders;

namespace NeoOrders.Domain.Clients;

// KAN-38: definición persistible del cliente. Sin DataAnnotations ni dependencias de EF.
// El esquema se configura en Infrastructure/Data/Configurations/ClientConfiguration.
public class Client
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string? Direccion { get; set; }
    public DateTime FechaAlta { get; set; } = DateTime.UtcNow;

    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
