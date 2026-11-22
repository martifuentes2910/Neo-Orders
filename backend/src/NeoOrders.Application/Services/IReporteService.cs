using NeoOrders.Application.Contracts.Reportes;
using NeoOrders.Application.DTOs.Reportes;

namespace NeoOrders.Application.Services;

public interface IReporteService
{
    Task<ReporteResumenDto> GetResumenAsync(FiltrosReporte filtros);
}
