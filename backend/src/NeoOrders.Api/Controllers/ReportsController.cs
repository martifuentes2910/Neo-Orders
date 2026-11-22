using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrdersApi.Application.Contracts.Reportes;
using OrdersApi.Application.Services;

namespace OrdersApi.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly IReporteService _reporteService;
    private readonly IVentasVendedorService _ventasVendedorService;
    private readonly IProductosVendidosService _productosVendidosService;
    private readonly IClientesConMasComprasService _clientesConMasComprasService;

    public ReportsController(
        IReporteService reporteService,
        IVentasVendedorService ventasVendedorService,
        IProductosVendidosService productosVendidosService,
        IClientesConMasComprasService clientesConMasComprasService)
    {
        _reporteService = reporteService;
        _ventasVendedorService = ventasVendedorService;
        _productosVendidosService = productosVendidosService;
        _clientesConMasComprasService = clientesConMasComprasService;
    }

    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] DateTime? desde,
        [FromQuery] DateTime? hasta,
        [FromQuery] int? clientId,
        [FromQuery] int? sellerId,
        [FromQuery] string? estado)
    {
        var filtros = new FiltrosReporte { Desde = desde, Hasta = hasta, ClientId = clientId, SellerId = sellerId, Estado = estado };
        return Ok(await _reporteService.GetResumenAsync(filtros));
    }

    [HttpGet("ventas-vendedor")]
    public async Task<IActionResult> VentasVendedor([FromQuery] DateTime? desde, [FromQuery] DateTime? hasta, [FromQuery] int? sellerId, [FromQuery] string? estado, [FromQuery] int page = 1, [FromQuery] int pageSize = 25)
        => Ok(await _ventasVendedorService.ConsultarAsync(new FiltrosReporte { Desde = desde, Hasta = hasta, SellerId = sellerId, Estado = estado }, Paginacion.Crear(page, pageSize)));

    [HttpGet("productos-vendidos")]
    public async Task<IActionResult> ProductosVendidos([FromQuery] DateTime? desde, [FromQuery] DateTime? hasta, [FromQuery] string? estado, [FromQuery] int page = 1, [FromQuery] int pageSize = 25)
        => Ok(await _productosVendidosService.ConsultarAsync(new FiltrosReporte { Desde = desde, Hasta = hasta, Estado = estado }, Paginacion.Crear(page, pageSize)));

    [HttpGet("clientes-compras")]
    public async Task<IActionResult> ClientesCompras([FromQuery] DateTime? desde, [FromQuery] DateTime? hasta, [FromQuery] int? clientId, [FromQuery] string? estado, [FromQuery] int page = 1, [FromQuery] int pageSize = 25)
        => Ok(await _clientesConMasComprasService.ConsultarAsync(new FiltrosReporte { Desde = desde, Hasta = hasta, ClientId = clientId, Estado = estado }, Paginacion.Crear(page, pageSize)));
}
