using NeoOrders.Application.DTOs.Reportes;

namespace NeoOrders.Application.Services;

public interface IDashboardService
{
    Task<DashboardMetricsDto> GetMetricsAsync();
}
