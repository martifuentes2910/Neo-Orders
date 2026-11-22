using NeoOrders.Application.Contracts.Reportes;
using NeoOrders.Application.DTOs.Reportes;

namespace NeoOrders.Application.Services;

public sealed class DashboardService : IDashboardService
{
    private readonly IDashboardMetricsRepository _repository;
    public DashboardService(IDashboardMetricsRepository repository) => _repository = repository;

    public Task<DashboardMetricsDto> GetMetricsAsync() => _repository.GetMetricsAsync();
}
