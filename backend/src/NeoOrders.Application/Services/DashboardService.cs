using OrdersApi.Application.Contracts.Reportes;
using OrdersApi.Application.DTOs.Reportes;

namespace OrdersApi.Application.Services;

public sealed class DashboardService : IDashboardService
{
    private readonly IDashboardMetricsRepository _repository;
    public DashboardService(IDashboardMetricsRepository repository) => _repository = repository;

    public Task<DashboardMetricsDto> GetMetricsAsync() => _repository.GetMetricsAsync();
}
