using Microsoft.EntityFrameworkCore;
using NeoOrders.Application.Contracts.Reportes;
using NeoOrders.Application.DTOs.Reportes;
using NeoOrders.Infrastructure.Models;

namespace NeoOrders.Infrastructure.Data.Repositories.Reportes;

/// <summary>Las cinco agregaciones del resumen. Contrato identico al anterior ReportService.</summary>
public sealed class ReporteResumenRepository : IReporteResumenRepository
{
    private readonly AppDbContext _db;
    public ReporteResumenRepository(AppDbContext db) => _db = db;

    public async Task<ReporteResumenDto> GetResumenAsync(FiltrosReporte filtros)
    {
        var q = ReporteQueryHelper.AplicarFiltros(_db.Orders.AsNoTracking().AsQueryable(), filtros);

        var porEstado = await q
            .GroupBy(p => p.Estado)
            .Select(g => new EstadoResumenDto { Estado = g.Key, Cantidad = g.Count(), Total = g.Sum(p => p.Total) })
            .ToListAsync();

        var porMes = await q
            .GroupBy(p => new { p.Fecha.Year, p.Fecha.Month })
            .Select(g => new MesResumenDto { Year = g.Key.Year, Month = g.Key.Month, Cantidad = g.Count(), Ingresos = g.Sum(p => p.Total) })
            .OrderBy(x => x.Year).ThenBy(x => x.Month)
            .ToListAsync();

        var topProductos = await ReporteQueryHelper.ExcluirCancelados(q)
            .GroupBy(p => p.ProductName)
            .Select(g => new ProductoResumenDto { Producto = g.Key, Cantidad = g.Sum(p => p.Cantidad), Ingresos = g.Sum(p => p.Total) })
            .OrderByDescending(x => x.Ingresos)
            .Take(5)
            .ToListAsync();

        var topClientes = await ReporteQueryHelper.ExcluirCancelados(q)
            .GroupBy(p => new { p.ClientId, Nombre = p.Client!.Nombre })
            .Select(g => new ClienteResumenDto { ClienteId = g.Key.ClientId, Cliente = g.Key.Nombre, Pedidos = g.Count(), Total = g.Sum(p => p.Total) })
            .OrderByDescending(x => x.Total)
            .Take(5)
            .ToListAsync();

        var ventasPorVendedor = await ReporteQueryHelper.ExcluirCancelados(q)
            .Where(p => p.SellerId != null)
            .GroupBy(p => new { p.SellerId, Nombre = p.Seller!.Name, p.Seller.CommissionPercentage })
            .Select(g => new VendedorResumenDto
            {
                VendedorId = g.Key.SellerId!.Value,
                Vendedor = g.Key.Nombre,
                Comision = g.Key.CommissionPercentage,
                Pedidos = g.Count(),
                Total = g.Sum(p => p.Total),
                ComisionEstimada = g.Sum(p => p.Total) * (g.Key.CommissionPercentage / 100m)
            })
            .OrderByDescending(x => x.Total)
            .ToListAsync();

        return new ReporteResumenDto
        {
            PorEstado = porEstado,
            PorMes = porMes,
            TopProductos = topProductos,
            TopClientes = topClientes,
            VentasPorVendedor = ventasPorVendedor
        };
    }
}
