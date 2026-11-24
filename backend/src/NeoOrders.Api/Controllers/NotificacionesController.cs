using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NeoOrders.Application.Contracts.Notifications;

namespace NeoOrders.Api.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificacionesController : ControllerBase
{
    private readonly INotificationService _notifications;
    public NotificacionesController(INotificationService notifications) => _notifications = notifications;

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int take = 60)
        => Ok(await _notifications.GetRecentAsync(take));
}
