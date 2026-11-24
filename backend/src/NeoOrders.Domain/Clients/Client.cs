using NeoOrders.Domain.Orders;

namespace NeoOrders.Domain.Clients;

// KAN-38: definición persistible del cliente. Sin DataAnnotations ni dependencias de EF.
// El esquema se configura en Infrastructure/Data/Configurations/ClientConfiguration.
// Los setters son privados: las altas usan Create/Rehydrate y las ediciones UpdateDetails.
public class Client
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

    public static Client Create(
        string nombre,
        string email,
        string? telefono = null,
        string? direccion = null,
        DateTime? fechaAlta = null) => Build(0, nombre, email, telefono, direccion, fechaAlta);

    // Reconstrucción desde persistencia/fixtures: no revalida para no rechazar filas legacy.
    public static Client Rehydrate(
        int id,
        string nombre,
        string email,
        string? telefono = null,
        string? direccion = null,
        DateTime? fechaAlta = null) => Build(id, nombre, email, telefono, direccion, fechaAlta);

    public void UpdateDetails(string nombre, string email, string? telefono, string? direccion)
    {
        Nombre = nombre;
        Email = email;
        Telefono = telefono;
        Direccion = direccion;
    }

    private static Client Build(
        int id,
        string nombre,
        string email,
        string? telefono,
        string? direccion,
        DateTime? fechaAlta) => new()
        {
            Id = id,
            Nombre = nombre,
            Email = email,
            Telefono = telefono,
            Direccion = direccion,
            FechaAlta = fechaAlta ?? DateTime.UtcNow
        };
}
