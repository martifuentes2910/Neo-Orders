namespace NeoOrders.Domain.Notifications;

public partial class Notification
{
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
