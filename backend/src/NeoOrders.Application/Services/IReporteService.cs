using OrdersApi.Application.Contracts.Reportes;
using OrdersApi.Application.DTOs.Reportes;

namespace OrdersApi.Application.Services;

public interface IReporteService
{
    Task<ReporteResumenDto> GetResumenAsync(FiltrosReporte filtros);
}
