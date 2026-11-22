using OrdersApi.Application.Contracts.Reportes;
using OrdersApi.Application.DTOs.Reportes;

namespace OrdersApi.Application.Services;

public interface IVentasVendedorService
{
    Task<VentasVendedorResponse> ConsultarAsync(FiltrosReporte filtros, Paginacion paginacion);
}

public sealed class VentasVendedorService : IVentasVendedorService
{
    private readonly IVentasVendedorRepository _repository;
    public VentasVendedorService(IVentasVendedorRepository repository) => _repository = repository;

    public Task<VentasVendedorResponse> ConsultarAsync(FiltrosReporte filtros, Paginacion paginacion) =>
        _repository.ConsultarAsync(filtros, paginacion);
}
