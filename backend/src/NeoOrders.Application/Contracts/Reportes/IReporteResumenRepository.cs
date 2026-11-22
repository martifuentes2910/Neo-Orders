using NeoOrders.Application.DTOs.Reportes;

namespace NeoOrders.Application.Contracts.Reportes;

public interface IReporteResumenRepository
{
    Task<ReporteResumenDto> GetResumenAsync(FiltrosReporte filtros);
}
