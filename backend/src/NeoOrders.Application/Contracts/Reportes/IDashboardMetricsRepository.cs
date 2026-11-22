using NeoOrders.Application.DTOs.Reportes;

namespace NeoOrders.Application.Contracts.Reportes;

public interface IDashboardMetricsRepository
{
    Task<DashboardMetricsDto> GetMetricsAsync();
}
