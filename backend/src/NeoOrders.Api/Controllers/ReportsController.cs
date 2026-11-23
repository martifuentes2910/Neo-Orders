using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NeoOrders.Application.Contracts.Reportes;
using NeoOrders.Application.Services;

namespace NeoOrders.Api.Controllers;

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
    {
        try
        {
            var paginacion = Paginacion.Crear(page, pageSize);
            var filtros = new FiltrosReporte { Desde = desde, Hasta = hasta, SellerId = sellerId, Estado = estado };
            return Ok(await _ventasVendedorService.ConsultarAsync(filtros, paginacion));
        }
        catch (PaginacionInvalidaException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("productos-vendidos")]
    public async Task<IActionResult> ProductosVendidos([FromQuery] DateTime? desde, [FromQuery] DateTime? hasta, [FromQuery] string? estado, [FromQuery] int page = 1, [FromQuery] int pageSize = 25)
    {
        try
        {
            var paginacion = Paginacion.Crear(page, pageSize);
            var filtros = new FiltrosReporte { Desde = desde, Hasta = hasta, Estado = estado };
            return Ok(await _productosVendidosService.ConsultarAsync(filtros, paginacion));
        }
        catch (PaginacionInvalidaException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("clientes-compras")]
    public async Task<IActionResult> ClientesCompras([FromQuery] DateTime? desde, [FromQuery] DateTime? hasta, [FromQuery] int? clientId, [FromQuery] string? estado, [FromQuery] int page = 1, [FromQuery] int pageSize = 25)
    {
        try
        {
            var paginacion = Paginacion.Crear(page, pageSize);
            var filtros = new FiltrosReporte { Desde = desde, Hasta = hasta, ClientId = clientId, Estado = estado };
            return Ok(await _clientesConMasComprasService.ConsultarAsync(filtros, paginacion));
        }
        catch (PaginacionInvalidaException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
