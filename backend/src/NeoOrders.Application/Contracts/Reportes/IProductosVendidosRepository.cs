using NeoOrders.Application.DTOs.Reportes;

namespace NeoOrders.Application.Contracts.Reportes;

public interface IProductosVendidosRepository
{
    Task<ProductosVendidosResponse> ConsultarAsync(FiltrosReporte filtros, Paginacion paginacion);
}
