using OrdersApi.Application.DTOs.Reportes;

namespace OrdersApi.Application.Contracts.Reportes;

public interface IVentasVendedorRepository
{
    Task<VentasVendedorResponse> ConsultarAsync(FiltrosReporte filtros, Paginacion paginacion);
}
