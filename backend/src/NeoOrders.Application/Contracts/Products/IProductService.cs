using NeoOrders.Application.DTOs.Products;
using NeoOrders.Application.DTOs.Productos;

namespace NeoOrders.Application.Contracts.Products;

public interface IProductService
{
    Task<List<ProductDto>> ListAsync(string? search, string? category, bool? onlyInStock);
    Task<List<string>> CategoriesAsync();
    Task<ProductDto?> GetByIdAsync(int id);
    Task<ProductDto> CreateAsync(ProductDto dto);
    Task<bool> UpdateAsync(int id, ProductDto dto);
    Task<bool> DeleteAsync(int id);
    Task<(bool Ok, string? Error, string? ImageUrl)> UploadImageAsync(int id, byte[] content, string fileName, string? contentType, CancellationToken ct);
    Task<(bool Ok, string? Error)> RemoveImageAsync(int id, CancellationToken ct);
    Task<PriceAdjustmentPreviewResponse> PreviewPriceAdjustmentAsync(decimal percent);
    Task<PriceAdjustmentApplyResponse> ApplyPriceAdjustmentAsync(decimal percent);
}
