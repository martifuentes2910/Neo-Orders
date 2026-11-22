using OrdersApi.Application.DTOs.Import;

namespace OrdersApi.Services;

public interface ICsvImportService
{
    Task<CsvImportResultDto> ImportClientsAsync(Stream csvStream, CancellationToken ct = default);
    Task<CsvImportResultDto> ImportProductsAsync(Stream csvStream, CancellationToken ct = default);
    Task<CsvImportResultDto> ImportOrdersAsync(Stream csvStream, CancellationToken ct = default);
}
