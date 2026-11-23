namespace NeoOrders.Application.DTOs.Notifications;

public class NotificationDto
{
    public int Id { get; set; }
    public string Tipo { get; set; } = "sistema";
    public string Accion { get; set; } = "actualizado";
    public string Titulo { get; set; } = string.Empty;
    public string Mensaje { get; set; } = string.Empty;
    public string? Link { get; set; }
    public string? Usuario { get; set; }
    public DateTime Fecha { get; set; }
    public int? EntidadId { get; set; }
}

public class NotificationsSummaryDto
{
    public int Total { get; set; }
    public int Hoy { get; set; }
    public int StockCritico { get; set; }
    public int Pendientes { get; set; }
}

public class NotificationsResponseDto
{
    public List<NotificationDto> Items { get; set; } = [];
    public NotificationsSummaryDto Resumen { get; set; } = new();
}
