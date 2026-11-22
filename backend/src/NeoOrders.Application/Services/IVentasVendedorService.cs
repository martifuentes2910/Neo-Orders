using NeoOrders.Application.Contracts.Reportes;
using NeoOrders.Application.DTOs.Reportes;

namespace NeoOrders.Application.Services;

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
