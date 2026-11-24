namespace NeoOrders.Domain.Notifications;

// KAN-38: definición persistible de la notificación. Sin DataAnnotations ni dependencias de EF.
// El esquema se configura en Infrastructure/Data/Configurations/NotificationConfiguration.
public class Notification
{
    public int Id { get; set; }

    /// <summary>cliente, pedido, producto, vendedor, usuario</summary>
    public string Tipo { get; set; } = "sistema";

    /// <summary>creado, actualizado, eliminado</summary>
    public string Accion { get; set; } = "actualizado";

    public string Titulo { get; set; } = string.Empty;
    public string Mensaje { get; set; } = string.Empty;
    public string? Link { get; set; }
    public string? Usuario { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    public int? EntidadId { get; set; }
}
