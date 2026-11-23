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
        if (s == null) return NotFound();
        return Ok(s);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] SellerDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var created = await _sellers.CreateAsync(dto);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] SellerDto dto)
    {
        if (id != dto.Id) return BadRequest();
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var ok = await _sellers.UpdateAsync(id, dto);
        if (!ok) return NotFound();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var (ok, error) = await _sellers.DeleteAsync(id);
        if (!ok && error == "NOT_FOUND") return NotFound();
        if (!ok) return BadRequest(new { message = error });
        return NoContent();
    }
}
