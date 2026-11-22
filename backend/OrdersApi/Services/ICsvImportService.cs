using OrdersApi.Application.DTOs.Import;

namespace OrdersApi.Services;

public interface ICsvImportService
{
    Task<CsvImportResultDto> ImportClientsAsync(IFormFile file, CancellationToken ct = default);
    Task<CsvImportResultDto> ImportProductsAsync(IFormFile file, CancellationToken ct = default);
    Task<CsvImportResultDto> ImportOrdersAsync(IFormFile file, CancellationToken ct = default);
}
