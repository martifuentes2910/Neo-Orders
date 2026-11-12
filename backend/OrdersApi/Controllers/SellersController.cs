using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrdersApi.Data;
using OrdersApi.Models;

namespace OrdersApi.Controllers;

[ApiController]
[Route("api/sellers")]
[Authorize]
public class SellersController : ControllerBase
{
    private readonly AppDbContext _db;
    public SellersController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] bool? onlyActive)
    {
        var q = _db.Sellers.AsQueryable();
        if (onlyActive == true) q = q.Where(s => s.Active);
        return Ok(await q.OrderBy(s => s.Name).ToListAsync());
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var s = await _db.Sellers.FindAsync(id);
        if (s == null) return NotFound();
        return Ok(s);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] Seller dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        _db.Sellers.Add(dto);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = dto.Id }, dto);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] Seller dto)
    {
        if (id != dto.Id) return BadRequest();
        var existing = await _db.Sellers.FindAsync(id);
        if (existing == null) return NotFound();
        existing.Name = dto.Name;
        existing.Email = dto.Email;
        existing.Phone = dto.Phone;
        existing.CommissionPercentage = dto.CommissionPercentage;
        existing.Active = dto.Active;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var s = await _db.Sellers.Include(x => x.Orders).FirstOrDefaultAsync(x => x.Id == id);
        if (s == null) return NotFound();
        if (s.Orders.Any()) return BadRequest(new { message = "El vendedor tiene pedidos asociados" });
        _db.Sellers.Remove(s);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
