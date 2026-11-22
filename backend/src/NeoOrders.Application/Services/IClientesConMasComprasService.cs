using NeoOrders.Application.Contracts.Reportes;
using NeoOrders.Application.DTOs.Reportes;

namespace NeoOrders.Application.Services;

public interface IClientesConMasComprasService
{
    Task<ClientesComprasResponse> ConsultarAsync(FiltrosReporte filtros, Paginacion paginacion);
}

public sealed class ClientesConMasComprasService : IClientesConMasComprasService
{
    private readonly IClientesConMasComprasRepository _repository;
    public ClientesConMasComprasService(IClientesConMasComprasRepository repository) => _repository = repository;

    public Task<ClientesComprasResponse> ConsultarAsync(FiltrosReporte filtros, Paginacion paginacion) =>
        _repository.ConsultarAsync(filtros, paginacion);
}
