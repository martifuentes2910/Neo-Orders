using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NeoOrders.Application.Contracts.Clients;
using NeoOrders.Application.Contracts.Import;
using NeoOrders.Application.DTOs.Clients;

namespace NeoOrders.Api.Controllers;

[ApiController]
[Route("api/clients")]
[Authorize]
public class ClientsController : ControllerBase
{
    private readonly IClientService _clients;
    private readonly ICsvImportService _csvImport;
    public ClientsController(IClientService clients, ICsvImportService csvImport) { _clients = clients; _csvImport = csvImport; }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? search) => Ok(await _clients.ListAsync(search));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var c = await _clients.GetByIdAsync(id);
        if (c == null) return NotFound(new { message = "Cliente no encontrado." });
        return Ok(c);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] ClientDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        try
        {
            var created = await _clients.CreateAsync(dto);
            return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] ClientDto dto)
    {
        if (id != dto.Id) return BadRequest(new { message = "El ID de la ruta no coincide con el cuerpo." });
        if (!ModelState.IsValid) return BadRequest(ModelState);
        try
        {
            var ok = await _clients.UpdateAsync(id, dto);
            if (!ok) return NotFound(new { message = "Cliente no encontrado." });
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
        var (ok, error) = await _clients.DeleteAsync(id);
        if (!ok && error == "NOT_FOUND") return NotFound(new { message = "Cliente no encontrado." });
        if (!ok) return BadRequest(new { message = error });
        return NoContent();
    }

    [HttpGet("import-csv/template")]
    [Authorize(Roles = "Admin")]
    public IActionResult Template() => File(System.Text.Encoding.UTF8.GetBytes("Name;Email;Phone;Address\n"), "text/csv; charset=utf-8", "plantilla-clientes.csv");

    [HttpPost("import-csv")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ImportCsv([FromForm] IFormFile? file, CancellationToken ct)
    {
        if (file == null) return BadRequest(new { message = "Debe seleccionar un archivo CSV." });
        var ext = Path.GetExtension(file.FileName);
        if (!string.Equals(ext, ".csv", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "El archivo debe tener extensión .csv." });
        await using var stream = file.OpenReadStream();
        var result = await _csvImport.ImportClientsAsync(stream, ct);
        if (result.Errors.Count > 0) return BadRequest(result);
        return Ok(result);
    }
}
