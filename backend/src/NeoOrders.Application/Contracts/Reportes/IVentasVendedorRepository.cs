using NeoOrders.Application.DTOs.Reportes;

namespace NeoOrders.Application.Contracts.Reportes;

public interface IVentasVendedorRepository
{
    Task<VentasVendedorResponse> ConsultarAsync(FiltrosReporte filtros, Paginacion paginacion);
}
