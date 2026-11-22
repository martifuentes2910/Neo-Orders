using Microsoft.EntityFrameworkCore;
using OrdersApi.Application.Contracts.Reportes;
using OrdersApi.Application.DTOs.Reportes;

namespace OrdersApi.Data.Repositories.Reportes;

/// <summary>Productos mas vendidos: agrupa por ProductName historico, igual que el resumen.</summary>
public sealed class ProductosVendidosRepository : IProductosVendidosRepository
{
    private readonly AppDbContext _db;
    public ProductosVendidosRepository(AppDbContext db) => _db = db;

    public async Task<ProductosVendidosResponse> ConsultarAsync(FiltrosReporte filtros, Paginacion paginacion)
    {
        var query = ReporteQueryHelper.ExcluirCancelados(
            ReporteQueryHelper.AplicarFiltros(_db.Orders.AsNoTracking().AsQueryable(), filtros));

        var todas = await query
            .GroupBy(p => p.ProductName)
            .Select(g => new FilaProductoDto
            {
                Producto = g.Key,
                Pedidos = g.Count(),
                Unidades = g.Sum(p => p.Cantidad),
                Ingresos = g.Sum(p => p.Total)
            })
            .ToListAsync();

        todas = todas.OrderByDescending(x => x.Ingresos).ThenBy(x => x.Producto, StringComparer.Ordinal).ToList();

        var denominador = todas.Sum(x => x.Ingresos);
        foreach (var fila in todas)
            fila.Participacion = ReporteQueryHelper.CalcularParticipacion(fila.Ingresos, denominador);

        var totalFilas = todas.Count;
        return new ProductosVendidosResponse
        {
            TotalFilas = totalFilas,
            TotalPaginas = ReporteQueryHelper.CalcularTotalPaginas(totalFilas, paginacion.TamanoPagina),
            Pagina = paginacion.Pagina,
            TamanoPagina = paginacion.TamanoPagina,
            FilasGrafico = todas,
            Filas = todas.Skip((paginacion.Pagina - 1) * paginacion.TamanoPagina)
                         .Take(paginacion.TamanoPagina).ToList()
        };
    }
}
