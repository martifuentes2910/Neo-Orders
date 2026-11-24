namespace NeoOrders.Domain.Notifications;

// KAN-38: definición persistible de la notificación. Sólo propiedades;
// el comportamiento vive en Notification.Behavior.cs. Sin dependencias de EF/AspNet/Identity/DataAnnotations.
// El esquema se configura en Infrastructure/Data/Configurations/NotificationConfiguration.
// Los setters son privados: el alta se hace con Create/Rehydrate (las notificaciones son inmutables).
public partial class Notification
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
}
