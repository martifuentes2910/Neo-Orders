using OrdersApi.Application.DTOs.Reportes;

namespace OrdersApi.Application.Contracts.Reportes;

public interface IProductosVendidosRepository
{
    Task<ProductosVendidosResponse> ConsultarAsync(FiltrosReporte filtros, Paginacion paginacion);
}
