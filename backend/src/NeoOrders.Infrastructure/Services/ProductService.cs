using Microsoft.EntityFrameworkCore;
using NeoOrders.Application.Contracts.Productos;
using NeoOrders.Application.Contracts.Products;
using NeoOrders.Application.DTOs.Productos;
using NeoOrders.Application.DTOs.Products;
using NeoOrders.Application.Services;
using NeoOrders.Domain.Products;
using NeoOrders.Infrastructure.Data;

namespace NeoOrders.Infrastructure.Services;

public class ProductService : IProductService
{
    private readonly AppDbContext _db;
    private readonly IProductImageStorage _imageStorage;
    public ProductService(AppDbContext db, IProductImageStorage imageStorage) { _db = db; _imageStorage = imageStorage; }

    private static bool IsUniqueConstraint(DbUpdateException ex)
        => ex.InnerException?.Message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase) == true
           || ex.InnerException?.Message.Contains("duplicate", StringComparison.OrdinalIgnoreCase) == true;

    public async Task<List<ProductDto>> ListAsync(string? search, string? category, bool? onlyInStock)
    {
        var q = _db.Products.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            q = q.Where(p => p.Name.ToLower().Contains(s) || p.Sku.ToLower().Contains(s));
        }
        if (!string.IsNullOrWhiteSpace(category) && category != "Todos") q = q.Where(p => p.Category == category);
        if (onlyInStock == true) q = q.Where(p => p.Stock > 0);
        return await q.OrderBy(p => p.Name).Select(p => Map(p)).ToListAsync();
    }

    public Task<List<string>> CategoriesAsync() => _db.Products.AsNoTracking().Select(p => p.Category).Distinct().OrderBy(c => c).ToListAsync();

    public async Task<ProductDto?> GetByIdAsync(int id) => await _db.Products.AsNoTracking().Where(p => p.Id == id).Select(p => Map(p)).FirstOrDefaultAsync();

    public async Task<ProductDto> CreateAsync(ProductDto dto)
    {
        var entity = Product.Create(
            sku: dto.Sku.Trim(),
            name: dto.Name,
            category: dto.Category,
            basePrice: dto.BasePrice,
            cost: dto.Cost,
            stock: dto.Stock,
            minStockAlert: dto.MinStockAlert,
            active: dto.Active);
        _db.Products.Add(entity);
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (IsUniqueConstraint(ex))
        {
            throw new InvalidOperationException("Ya existe un producto con ese SKU.");
        }
        return Map(entity);
    }

    public async Task<bool> UpdateAsync(int id, ProductDto dto)
    {
        var existing = await _db.Products.FindAsync(id);
        if (existing == null) return false;
        existing.UpdateDetails(
            dto.Sku.Trim(), dto.Name, dto.Category, dto.BasePrice, dto.Cost,
            dto.Stock, dto.MinStockAlert, dto.Active);
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (IsUniqueConstraint(ex))
        {
            throw new InvalidOperationException("Ya existe un producto con ese SKU.");
        }
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var p = await _db.Products.FindAsync(id);
        if (p == null) return false;
        var publicId = p.ImagePublicId;
        _db.Products.Remove(p);
        await _db.SaveChangesAsync();
        await _imageStorage.DeleteAsync(publicId);
        return true;
    }

    public async Task<(bool Ok, string? Error, string? ImageUrl)> UploadImageAsync(int id, byte[] content, string fileName, string? contentType, CancellationToken ct)
    {
        if (!_imageStorage.IsConfigured) return (false, "Almacenamiento de imágenes no configurado. Definí Cloudinary__CloudName, Cloudinary__ApiKey y Cloudinary__ApiSecret.", null);
        var error = ProductImageValidator.Validate(fileName, contentType, content);
        if (error != null) return (false, error, null);
        var p = await _db.Products.FindAsync([id], ct);
        if (p == null) return (false, "NOT_FOUND", null);
        var oldPublicId = p.ImagePublicId;
        var stored = await _imageStorage.UploadAsync(content, fileName, ct);
        p.SetImage(stored.Url, stored.PublicId);
        await _db.SaveChangesAsync(ct);
        if (!string.IsNullOrWhiteSpace(oldPublicId) && oldPublicId != stored.PublicId)
            await _imageStorage.DeleteAsync(oldPublicId, ct);
        return (true, null, p.ImageUrl);
    }

    public async Task<(bool Ok, string? Error)> RemoveImageAsync(int id, CancellationToken ct)
    {
        var p = await _db.Products.FindAsync([id], ct);
        if (p == null) return (false, "NOT_FOUND");
        var oldPublicId = p.ImagePublicId;
        p.ClearImage();
        await _db.SaveChangesAsync(ct);
        await _imageStorage.DeleteAsync(oldPublicId, ct);
        return (true, null);
    }

    public async Task<PriceAdjustmentPreviewResponse> PreviewPriceAdjustmentAsync(decimal percent)
    {
        var factor = 1m + (percent / 100m);
        if (factor <= 0) return new PriceAdjustmentPreviewResponse { Percent = percent, Factor = factor, Errors = ["El factor resultante debe ser mayor a 0."] };
        var active = await _db.Products.AsNoTracking().Where(p => p.Active).OrderBy(p => p.Name).ToListAsync();
        var skipped = await _db.Products.AsNoTracking().CountAsync(p => !p.Active);
        var items = active.Select(p => new PriceAdjustmentItemDto { Id = p.Id, Sku = p.Sku, Name = p.Name, CurrentBasePrice = p.BasePrice, NewBasePrice = Math.Round(p.BasePrice * factor, 2) }).ToList();
        return new PriceAdjustmentPreviewResponse { Percent = percent, Factor = factor, AffectedCount = items.Count, SkippedInactiveCount = skipped, Items = items };
    }

    public async Task<PriceAdjustmentApplyResponse> ApplyPriceAdjustmentAsync(decimal percent)
    {
        var factor = 1m + (percent / 100m);
        if (factor <= 0) throw new InvalidOperationException("El factor resultante debe ser mayor a 0.");
        var active = await _db.Products.Where(p => p.Active).ToListAsync();
        foreach (var p in active) p.SetBasePrice(Math.Round(p.BasePrice * factor, 2));
        await _db.SaveChangesAsync();
        return new PriceAdjustmentApplyResponse { AffectedCount = active.Count, Percent = percent, Factor = factor, AppliedAtUtc = DateTime.UtcNow };
    }

    private static ProductDto Map(Product p) => new()
    {
        Id = p.Id, Sku = p.Sku, Name = p.Name, Category = p.Category, BasePrice = p.BasePrice, Cost = p.Cost, Stock = p.Stock,
        MinStockAlert = p.MinStockAlert, Active = p.Active, CreatedAt = p.CreatedAt, IsCriticalStock = p.IsCriticalStock, ImageUrl = p.ImageUrl
    };
}
