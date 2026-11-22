using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrdersApi.Data;
using OrdersApi.Models;

namespace OrdersApi.Controllers;

[ApiController]
[Route("api/clients")]
[Authorize]
public class ClientsController : ControllerBase
{
    private readonly AppDbContext _db;
    public ClientsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? search)
    {
        var q = _db.Clients.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            q = q.Where(c => c.Nombre.ToLower().Contains(s) || c.Email.ToLower().Contains(s));
        }
        var list = await q.OrderBy(c => c.Nombre).Select(c => new
        {
            c.Id,
            c.Nombre,
            c.Email,
            c.Telefono,
            c.Direccion,
            c.FechaAlta,
            OrdersCount = c.Orders.Count,
            TotalSpent = c.Orders.Sum(o => (decimal?)o.Total) ?? 0m
        }).ToListAsync();
        return Ok(list);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var c = await _db.Clients.FindAsync(id);
        if (c == null) return NotFound();
        return Ok(c);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] Client dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        _db.Clients.Add(dto);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = dto.Id }, dto);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] Client dto)
    {
        if (id != dto.Id) return BadRequest();
        var existing = await _db.Clients.FindAsync(id);
        if (existing == null) return NotFound();
        existing.Nombre = dto.Nombre;
        existing.Email = dto.Email;
        existing.Telefono = dto.Telefono;
        existing.Direccion = dto.Direccion;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var c = await _db.Clients.Include(x => x.Orders).FirstOrDefaultAsync(x => x.Id == id);
        if (c == null) return NotFound();
        if (c.Orders.Any()) return BadRequest(new { message = "El cliente tiene pedidos asociados" });
        _db.Clients.Remove(c);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
