using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NeoOrders.Infrastructure.Data;
using NeoOrders.Infrastructure.Models;
using NeoOrders.Application.Contracts.Orders;
using NeoOrders.Application.Contracts.Notifications;
using NeoOrders.Application.Contracts.Import;
using NeoOrders.Domain.Orders;
using NeoOrders.Application.DTOs.Orders;

namespace NeoOrders.Api.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IOrderService _orderService;
    private readonly IOrderQueryService _orderQuery;
    private readonly ICsvImportService _csvImport;
    public OrdersController(AppDbContext db, IOrderService orderService, IOrderQueryService orderQuery, ICsvImportService csvImport)
    {
        _db = db;
        _orderService = orderService;
        _orderQuery = orderQuery;
        _csvImport = csvImport;
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? search, [FromQuery] string? estado, [FromQuery] int? clientId, [FromQuery] int? sellerId)
    {
        var list = await _orderQuery.ListAsync(search, estado, clientId, sellerId);
        return Ok(list);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var p = await _orderQuery.GetByIdAsync(id);
        if (p == null) return NotFound();
        return Ok(p);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrderDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var result = await _orderService.CreateAsync(dto.ClientId, dto.SellerId, dto.ProductId, dto.Cantidad, dto.OrderNumber);
        if (!result.Success) return BadRequest(new { message = result.Error });
        return CreatedAtAction(nameof(Get), new { id = result.OrderId }, new { id = result.OrderId });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateOrderDto dto)
    {
        if (id != dto.Id) return BadRequest();
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var current = await _db.Orders.AsNoTracking().Where(o => o.Id == id).Select(o => o.Estado).FirstOrDefaultAsync();
        if (current != null && OrderStatuses.EditLocked.Contains(current))
        {
            var statusResult = await _orderService.UpdateStatusAsync(id, dto.Estado);
            if (statusResult.NotFound) return NotFound(new { message = statusResult.Error });
            if (!statusResult.Success) return BadRequest(new { message = statusResult.Error });
            return NoContent();
        }

        var result = await _orderService.UpdateAsync(id, dto.ClientId, dto.SellerId, dto.Cantidad, dto.Estado);
        if (result.NotFound) return NotFound(new { message = result.Error });
        if (!result.Success) return BadRequest(new { message = result.Error });
        return NoContent();
    }

    [HttpPatch("{id:int}/estado")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateOrderStatusDto dto)
    {
        var result = await _orderService.UpdateStatusAsync(id, dto.Estado);
        if (result.NotFound) return NotFound(new { message = result.Error });
        if (!result.Success) return BadRequest(new { message = result.Error });
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var ok = await _orderService.DeleteAsync(id);
        if (!ok) return NotFound();
        return NoContent();
    }

    [HttpGet("statuses")]
    public IActionResult Statuses() => Ok(OrderStatuses.All);

    [HttpGet("import-csv/template")]
    [Authorize(Roles = "Admin")]
    public IActionResult Template() => File(System.Text.Encoding.UTF8.GetBytes("OrderNumber;ClientId;SellerId;ProductId;Cantidad\n"), "text/csv; charset=utf-8", "plantilla.csv");

    [HttpPost("import-csv")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ImportCsv([FromForm] IFormFile file, CancellationToken ct)
    {
        if (file == null) return BadRequest(new { message = "Debe seleccionar un archivo CSV." });
        var ext = Path.GetExtension(file.FileName);
        if (!string.Equals(ext, ".csv", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "El archivo debe tener extensión .csv." });
        await using var stream = file.OpenReadStream();
        var result = await _csvImport.ImportOrdersAsync(stream, ct);
        if (result.Errors.Count > 0) return BadRequest(result);
        return Ok(result);
    }
}
