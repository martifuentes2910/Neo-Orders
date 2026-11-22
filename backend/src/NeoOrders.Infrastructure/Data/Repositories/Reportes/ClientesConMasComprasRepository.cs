using Microsoft.EntityFrameworkCore;
using OrdersApi.Application.Contracts.Reportes;
using OrdersApi.Application.DTOs.Reportes;

namespace OrdersApi.Data.Repositories.Reportes;

/// <summary>Clientes con mas compras: agrupa por ClientId + nombre.</summary>
public sealed class ClientesConMasComprasRepository : IClientesConMasComprasRepository
{
    private readonly AppDbContext _db;
    public ClientesConMasComprasRepository(AppDbContext db) => _db = db;

    public async Task<ClientesComprasResponse> ConsultarAsync(FiltrosReporte filtros, Paginacion paginacion)
    {
        var query = ReporteQueryHelper.ExcluirCancelados(
            ReporteQueryHelper.AplicarFiltros(_db.Orders.AsNoTracking().AsQueryable(), filtros));

        var todas = await query
            .GroupBy(p => new { p.ClientId, Nombre = p.Client!.Nombre })
            .Select(g => new FilaClienteDto
            {
                ClienteId = g.Key.ClientId,
                Cliente = g.Key.Nombre,
                Pedidos = g.Count(),
                Unidades = g.Sum(p => p.Cantidad),
                Ingresos = g.Sum(p => p.Total)
            })
            .ToListAsync();

        todas = todas.OrderByDescending(x => x.Ingresos).ThenBy(x => x.ClienteId).ToList();

        var denominador = todas.Sum(x => x.Ingresos);
        foreach (var fila in todas)
            fila.Participacion = ReporteQueryHelper.CalcularParticipacion(fila.Ingresos, denominador);

        var totalFilas = todas.Count;
        return new ClientesComprasResponse
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
