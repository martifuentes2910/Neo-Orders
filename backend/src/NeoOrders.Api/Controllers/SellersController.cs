using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NeoOrders.Application.Contracts.Sellers;
using NeoOrders.Application.DTOs.Sellers;

namespace NeoOrders.Api.Controllers;

[ApiController]
[Route("api/sellers")]
[Authorize]
public class SellersController : ControllerBase
{
    private readonly ISellerService _sellers;
    public SellersController(ISellerService sellers) => _sellers = sellers;

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] bool? onlyActive) => Ok(await _sellers.ListAsync(onlyActive));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var s = await _sellers.GetByIdAsync(id);
        if (s == null) return NotFound(new { message = "Vendedor no encontrado." });
        return Ok(s);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] SellerDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        try
        {
            var created = await _sellers.CreateAsync(dto);
            return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] SellerDto dto)
    {
        if (id != dto.Id) return BadRequest(new { message = "El ID de la ruta no coincide con el cuerpo." });
        if (!ModelState.IsValid) return BadRequest(ModelState);
        try
        {
            var ok = await _sellers.UpdateAsync(id, dto);
            if (!ok) return NotFound(new { message = "Vendedor no encontrado." });
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var (ok, error) = await _sellers.DeleteAsync(id);
        if (!ok && error == "NOT_FOUND") return NotFound(new { message = "Vendedor no encontrado." });
        if (!ok) return BadRequest(new { message = error });
        return NoContent();
    }
}
