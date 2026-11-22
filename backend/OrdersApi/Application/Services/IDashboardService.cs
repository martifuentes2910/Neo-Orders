using OrdersApi.Application.DTOs.Reportes;

namespace OrdersApi.Application.Services;

public interface IDashboardService
{
    Task<DashboardMetricsDto> GetMetricsAsync();
}
