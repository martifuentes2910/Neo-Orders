using OrdersApi.Application.Contracts.Reportes;
using OrdersApi.Application.DTOs.Reportes;

namespace OrdersApi.Application.Services;

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
