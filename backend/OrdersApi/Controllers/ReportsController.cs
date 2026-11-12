using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrdersApi.Services;

namespace OrdersApi.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;
    public ReportsController(IReportService reportService) => _reportService = reportService;

    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] DateTime? desde,
        [FromQuery] DateTime? hasta,
        [FromQuery] int? clientId,
        [FromQuery] int? sellerId,
        [FromQuery] string? estado)
    {
        var filtros = new FiltrosReporte
        {
            Desde = desde,
            Hasta = hasta,
            ClientId = clientId,
            SellerId = sellerId,
            Estado = estado
        };
        return Ok(await _reportService.GetReportsAsync(filtros));
    }
}
