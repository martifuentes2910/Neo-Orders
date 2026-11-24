using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NeoOrders.Application.Contracts.Import;
using NeoOrders.Application.Contracts.Products;
using NeoOrders.Application.DTOs.Productos;
using NeoOrders.Application.DTOs.Products;

namespace NeoOrders.Api.Controllers;

[ApiController]
[Route("api/products")]
[Authorize]
public class ProductsController : ControllerBase
{
    private readonly IProductService _products;
    private readonly ICsvImportService _csvImport;
    public ProductsController(IProductService products, ICsvImportService csvImport) { _products = products; _csvImport = csvImport; }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? search,
        [FromQuery] string? category,
        [FromQuery] bool? onlyInStock,
        [FromQuery] string? sku,
        [FromQuery] string? nombre)
        => Ok(await _products.ListAsync(search, category, onlyInStock, sku, nombre));

    [HttpGet("categories")]
    public async Task<IActionResult> Categories() => Ok(await _products.CategoriesAsync());

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var p = await _products.GetByIdAsync(id);
        if (p == null) return NotFound(new { message = "Producto no encontrado." });
        return Ok(p);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] ProductDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        try
        {
            var created = await _products.CreateAsync(dto);
            return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] ProductDto dto)
    {
        if (id != dto.Id) return BadRequest(new { message = "El ID de la ruta no coincide con el cuerpo." });
        if (!ModelState.IsValid) return BadRequest(ModelState);
        try
        {
            var ok = await _products.UpdateAsync(id, dto);
            if (!ok) return NotFound(new { message = "Producto no encontrado." });
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
        var ok = await _products.DeleteAsync(id);
        if (!ok) return NotFound(new { message = "Producto no encontrado." });
        return NoContent();
    }

    [HttpPost("{id:int}/image")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UploadImage(int id, IFormFile file, CancellationToken ct)
    {
        if (file == null) return BadRequest(new { message = "Debe seleccionar una imagen." });
        await using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);
        var (ok, error, imageUrl) = await _products.UploadImageAsync(id, ms.ToArray(), file.FileName, file.ContentType, ct);
        if (!ok && error == "NOT_FOUND") return NotFound(new { message = "Producto no encontrado." });
        if (!ok) return BadRequest(new { message = error });
        return Ok(new { imageUrl });
    }

    [HttpDelete("{id:int}/image")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RemoveImage(int id, CancellationToken ct)
    {
        var (ok, error) = await _products.RemoveImageAsync(id, ct);
        if (!ok && error == "NOT_FOUND") return NotFound(new { message = "Producto no encontrado." });
        if (!ok) return BadRequest(new { message = error });
        return NoContent();
    }

    [HttpPost("price-adjustment/preview")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> PreviewPriceAdjustment([FromBody] PriceAdjustmentRequest dto)
        => Ok(await _products.PreviewPriceAdjustmentAsync(dto.Percent));

    [HttpPost("price-adjustment/apply")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ApplyPriceAdjustment([FromBody] PriceAdjustmentRequest dto)
    {
        try { return Ok(await _products.ApplyPriceAdjustmentAsync(dto.Percent)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("import-csv/template")]
    [Authorize(Roles = "Admin")]
    public IActionResult Template() => File(System.Text.Encoding.UTF8.GetBytes("Sku;Name;Category;BasePrice;Cost;Stock;MinStockAlert;Active\n"), "text/csv; charset=utf-8", "plantilla.csv");

    [HttpPost("import-csv")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ImportCsv([FromForm] IFormFile? file, CancellationToken ct)
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
