namespace NeoOrders.Domain.Notifications;

// KAN-38: definición persistible de la notificación. Sin DataAnnotations ni dependencias de EF.
// El esquema se configura en Infrastructure/Data/Configurations/NotificationConfiguration.
// Los setters son privados: el alta se hace con Create/Rehydrate (las notificaciones son inmutables).
public class Notification
{
    public int Id { get; private set; }

    /// <summary>cliente, pedido, producto, vendedor, usuario</summary>
    public string Tipo { get; private set; } = "sistema";

    /// <summary>creado, actualizado, eliminado</summary>
    public string Accion { get; private set; } = "actualizado";

    public string Titulo { get; private set; } = string.Empty;
    public string Mensaje { get; private set; } = string.Empty;
    public string? Link { get; private set; }
    public string? Usuario { get; private set; }
    public DateTime Fecha { get; private set; } = DateTime.UtcNow;
    public int? EntidadId { get; private set; }

    // Constructor para la materialización de EF; el resto del código usa las factories.
    private Notification() { }

    public static Notification Create(
        string tipo,
        string accion,
        string titulo,
        string mensaje,
        string? link = null,
        string? usuario = null,
        int? entidadId = null,
        DateTime? fecha = null) => Build(0, tipo, accion, titulo, mensaje, link, usuario, entidadId, fecha);

    // Reconstrucción desde persistencia/fixtures: no revalida para no rechazar filas legacy.
    public static Notification Rehydrate(
        int id,
        string tipo,
        string accion,
        string titulo,
        string mensaje,
        string? link = null,
        string? usuario = null,
        int? entidadId = null,
        DateTime? fecha = null) => Build(id, tipo, accion, titulo, mensaje, link, usuario, entidadId, fecha);

    private static Notification Build(
        int id,
        string tipo,
        string accion,
        string titulo,
        string mensaje,
        string? link,
        string? usuario,
        int? entidadId,
        DateTime? fecha) => new()
        {
            Id = id,
            Tipo = tipo,
            Accion = accion,
            Titulo = titulo,
            Mensaje = mensaje,
            Link = link,
            Usuario = usuario,
            EntidadId = entidadId,
            Fecha = fecha ?? DateTime.UtcNow
        };
}
