using OrdersApi.Application.Contracts.Reportes;
using OrdersApi.Application.DTOs.Reportes;

namespace OrdersApi.Application.Services;

public interface IProductosVendidosService
{
    Task<ProductosVendidosResponse> ConsultarAsync(FiltrosReporte filtros, Paginacion paginacion);
}

public sealed class ProductosVendidosService : IProductosVendidosService
{
    private readonly IProductosVendidosRepository _repository;
    public ProductosVendidosService(IProductosVendidosRepository repository) => _repository = repository;

    public Task<ProductosVendidosResponse> ConsultarAsync(FiltrosReporte filtros, Paginacion paginacion) =>
        _repository.ConsultarAsync(filtros, paginacion);
}
