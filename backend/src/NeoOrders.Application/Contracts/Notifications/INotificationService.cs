namespace NeoOrders.Application.Contracts.Notifications;

public interface INotificationService
{
    Task PushAsync(string tipo, string accion, string titulo, string mensaje, string? link = null, int? entidadId = null);
}
