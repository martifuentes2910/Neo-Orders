using NeoOrders.Application.Contracts.Orders;
using NeoOrders.Application.Contracts.Notifications;
using NeoOrders.Application.Contracts.Import;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity;
using NeoOrders.Infrastructure.Data;
using NeoOrders.Infrastructure.Models;

namespace NeoOrders.Infrastructure.Services;

/// <summary>
/// Registra eventos de la aplicación (altas, cambios y bajas de registros).
/// Es best-effort: un fallo al guardar la notificación no debe romper el CRUD.
/// </summary>
public class NotificationService : INotificationService
{
    private readonly AppDbContext _db;
    private readonly IHttpContextAccessor _http;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        AppDbContext db,
        IHttpContextAccessor http,
        UserManager<ApplicationUser> userManager,
        ILogger<NotificationService> logger)
    {
        _db = db;
        _http = http;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task PushAsync(string tipo, string accion, string titulo, string mensaje, string? link = null, int? entidadId = null)
    {
        try
        {
            var actor = "Sistema";
            var principal = _http.HttpContext?.User;
            if (principal?.Identity?.IsAuthenticated == true)
            {
                var user = await _userManager.GetUserAsync(principal);
                if (user != null)
                {
                    var fullName = $"{user.Nombre} {user.Apellido}".Trim();
                    actor = string.IsNullOrWhiteSpace(fullName)
                        ? user.Email ?? user.UserName ?? "Usuario"
                        : fullName;
                }
            }

            _db.Notifications.Add(new Notification
            {
                Tipo = tipo,
                Accion = accion,
                Titulo = titulo,
                Mensaje = mensaje,
                Link = link,
                Usuario = actor,
                EntidadId = entidadId,
                Fecha = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo registrar la notificación: {Titulo}", titulo);
        }
    }
}
