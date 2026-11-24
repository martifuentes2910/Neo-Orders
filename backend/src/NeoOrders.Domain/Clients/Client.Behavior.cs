namespace NeoOrders.Domain.Clients;

public partial class Client
{
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
