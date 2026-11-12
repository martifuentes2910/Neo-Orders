using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrdersApi.Services;

namespace OrdersApi.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IReportService _reportService;
    public DashboardController(IReportService reportService) => _reportService = reportService;

    [HttpGet("metrics")]
    public async Task<IActionResult> Metrics() => Ok(await _reportService.GetDashboardMetricsAsync());
}
