using OrdersApi.Application.DTOs.Reportes;

namespace OrdersApi.Application.Contracts.Reportes;

public interface IReporteResumenRepository
{
    Task<ReporteResumenDto> GetResumenAsync(FiltrosReporte filtros);
}
