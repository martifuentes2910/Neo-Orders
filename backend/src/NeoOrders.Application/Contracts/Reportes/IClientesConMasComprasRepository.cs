using NeoOrders.Application.DTOs.Reportes;

namespace NeoOrders.Application.Contracts.Reportes;

public interface IClientesConMasComprasRepository
{
    Task<ClientesComprasResponse> ConsultarAsync(FiltrosReporte filtros, Paginacion paginacion);
}
