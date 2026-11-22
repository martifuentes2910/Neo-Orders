using OrdersApi.Application.DTOs.Reportes;

namespace OrdersApi.Application.Contracts.Reportes;

public interface IDashboardMetricsRepository
{
    Task<DashboardMetricsDto> GetMetricsAsync();
}
