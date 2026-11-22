using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrdersApi.Data;
using OrdersApi.Models;
using OrdersApi.Services;

namespace OrdersApi.Controllers;

[ApiController]
[Route("api/products")]
[Authorize]
public class ProductsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ICsvImportService _csvImport;
    public ProductsController(AppDbContext db, ICsvImportService csvImport) { _db = db; _csvImport = csvImport; }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? search, [FromQuery] string? category, [FromQuery] bool? onlyInStock)
    {
        var q = _db.Products.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            q = q.Where(p => p.Name.ToLower().Contains(s) || p.Sku.ToLower().Contains(s));
        }
        if (!string.IsNullOrWhiteSpace(category) && category != "Todos")
            q = q.Where(p => p.Category == category);
        if (onlyInStock == true)
            q = q.Where(p => p.Stock > 0);
        return Ok(await q.OrderBy(p => p.Name).ToListAsync());
    }

    [HttpGet("categories")]
    public async Task<IActionResult> Categories()
    {
        var cats = await _db.Products.Select(p => p.Category).Distinct().OrderBy(c => c).ToListAsync();
        return Ok(cats);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var p = await _db.Products.FindAsync(id);
        if (p == null) return NotFound();
        return Ok(p);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] Product dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        _db.Products.Add(dto);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = dto.Id }, dto);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] Product dto)
    {
        if (id != dto.Id) return BadRequest();
        var existing = await _db.Products.FindAsync(id);
        if (existing == null) return NotFound();
        existing.Sku = dto.Sku;
        existing.Name = dto.Name;
        existing.Category = dto.Category;
        existing.BasePrice = dto.BasePrice;
        existing.Cost = dto.Cost;
        existing.Stock = dto.Stock;
        existing.MinStockAlert = dto.MinStockAlert;
        existing.Active = dto.Active;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var p = await _db.Products.FindAsync(id);
        if (p == null) return NotFound();
        _db.Products.Remove(p);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("import-csv/template")]
    [Authorize(Roles = "Admin")]
    public IActionResult Template() => File(System.Text.Encoding.UTF8.GetBytes("Sku;Name;Category;BasePrice;Cost;Stock;MinStockAlert;Active\n"), "text/csv; charset=utf-8", "plantilla.csv");

    [HttpPost("import-csv")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ImportCsv([FromForm] IFormFile file, CancellationToken ct)
    {
        if (file == null) return BadRequest(new { message = "Debe seleccionar un archivo CSV." });
        var ext = Path.GetExtension(file.FileName);
        if (!string.Equals(ext, ".csv", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "El archivo debe tener extensión .csv." });
        await using var stream = file.OpenReadStream();
        var result = await _csvImport.ImportProductsAsync(stream, ct);
        if (result.Errors.Count > 0) return BadRequest(result);
        return Ok(result);
    }
}
