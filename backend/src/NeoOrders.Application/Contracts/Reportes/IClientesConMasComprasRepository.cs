using OrdersApi.Application.DTOs.Reportes;

namespace OrdersApi.Application.Contracts.Reportes;

public interface IClientesConMasComprasRepository
{
    Task<ClientesComprasResponse> ConsultarAsync(FiltrosReporte filtros, Paginacion paginacion);
}
