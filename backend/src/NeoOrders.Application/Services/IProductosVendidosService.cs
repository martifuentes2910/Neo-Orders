using NeoOrders.Application.Contracts.Reportes;
using NeoOrders.Application.DTOs.Reportes;

namespace NeoOrders.Application.Services;

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
