using Microsoft.EntityFrameworkCore;
using OrdersApi.Application.Contracts.Reportes;
using OrdersApi.Application.DTOs.Reportes;

namespace OrdersApi.Data.Repositories.Reportes;

/// <summary>Ventas por vendedor: una fila por vendedor, con comision y comision estimada.</summary>
public sealed class VentasVendedorRepository : IVentasVendedorRepository
{
    private readonly AppDbContext _db;
    public VentasVendedorRepository(AppDbContext db) => _db = db;

    public async Task<VentasVendedorResponse> ConsultarAsync(FiltrosReporte filtros, Paginacion paginacion)
    {
        var query = ReporteQueryHelper.ExcluirCancelados(
            ReporteQueryHelper.AplicarFiltros(_db.Orders.AsNoTracking().AsQueryable(), filtros));

        var todas = await query
            .Where(p => p.SellerId != null)
            .GroupBy(p => new { p.SellerId, Nombre = p.Seller!.Name, p.Seller.CommissionPercentage })
            .Select(g => new FilaVendedorDto
            {
                VendedorId = g.Key.SellerId!.Value,
                Vendedor = g.Key.Nombre,
                Pedidos = g.Count(),
                Unidades = g.Sum(p => p.Cantidad),
                Ingresos = g.Sum(p => p.Total),
                Comision = g.Key.CommissionPercentage,
                ComisionEstimada = g.Sum(p => p.Total) * (g.Key.CommissionPercentage / 100m)
            })
            .ToListAsync();

        todas = todas.OrderByDescending(x => x.Ingresos).ThenBy(x => x.VendedorId).ToList();

        var denominador = todas.Sum(x => x.Ingresos);
        foreach (var fila in todas)
            fila.Participacion = ReporteQueryHelper.CalcularParticipacion(fila.Ingresos, denominador);

        var totalFilas = todas.Count;
        return new VentasVendedorResponse
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
