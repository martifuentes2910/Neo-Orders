using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NeoOrders.Application.Contracts.Notifications;
using NeoOrders.Application.DTOs.Notifications;
using NeoOrders.Domain.Notifications;
using NeoOrders.Domain.Orders;
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

            _db.Notifications.Add(Notification.Create(
                tipo: tipo,
                accion: accion,
                titulo: titulo,
                mensaje: mensaje,
                link: link,
                usuario: actor,
                entidadId: entidadId,
                fecha: DateTime.UtcNow));
            await _db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo registrar la notificación: {Titulo}", titulo);
        }
    }


    public async Task<NotificationsResponseDto> GetRecentAsync(int take = 60)
    {
        take = Math.Clamp(take <= 0 ? 60 : take, 1, 200);
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        var items = await _db.Notifications
            .AsNoTracking()
            .OrderByDescending(n => n.Fecha)
            .Take(take)
            .Select(n => new NotificationDto
            {
                Id = n.Id,
                Tipo = n.Tipo,
                Accion = n.Accion,
                Titulo = n.Titulo,
                Mensaje = n.Mensaje,
                Link = n.Link,
                Usuario = n.Usuario,
                Fecha = n.Fecha,
                EntidadId = n.EntidadId
            })
            .ToListAsync();

        var stockCritico = await _db.Products.AsNoTracking()
            .CountAsync(p => p.Active && p.Stock <= p.MinStockAlert);
        var pendientes = await _db.Orders.AsNoTracking()
            .CountAsync(o => o.Estado == OrderStatuses.Pendiente);

        return new NotificationsResponseDto
        {
            Items = items,
            Resumen = new NotificationsSummaryDto
            {
                Total = items.Count,
                Hoy = items.Count(n => n.Fecha >= today && n.Fecha < tomorrow),
                StockCritico = stockCritico,
                Pendientes = pendientes
            }
        };
    }
}
