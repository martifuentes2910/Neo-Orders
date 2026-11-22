using OrdersApi.Application.Contracts.Reportes;
using OrdersApi.Application.DTOs.Reportes;

namespace OrdersApi.Application.Services;

/// <summary>Capa de aplicacion del resumen: solo delega en el repositorio.</summary>
public sealed class ReporteService : IReporteService
{
    private readonly IReporteResumenRepository _repository;
    public ReporteService(IReporteResumenRepository repository) => _repository = repository;

    public Task<ReporteResumenDto> GetResumenAsync(FiltrosReporte filtros) =>
        _repository.GetResumenAsync(filtros);
}
