using Microsoft.EntityFrameworkCore;
using NeoOrders.Application.Contracts.Reportes;
using NeoOrders.Application.DTOs.Reportes;
using NeoOrders.Domain.Orders;

namespace NeoOrders.Infrastructure.Data.Repositories.Reportes;

/// <summary>Metricas del panel. Sin filtros: el endpoint del panel no los expone.</summary>
public sealed class DashboardMetricsRepository : IDashboardMetricsRepository
{
    private readonly AppDbContext _db;
    public DashboardMetricsRepository(AppDbContext db) => _db = db;

    public async Task<DashboardMetricsDto> GetMetricsAsync()
    {
        var porEstado = await _db.Orders
            .AsNoTracking()
            .GroupBy(p => p.Estado)
            .Select(g => new EstadoResumenDto { Estado = g.Key, Cantidad = g.Count(), Total = g.Sum(p => p.Total) })
            .ToListAsync();

        var totalPedidos = porEstado.Sum(x => x.Cantidad);
        var ingresos = porEstado.Where(x => x.Estado != OrderStatuses.Cancelado).Sum(x => x.Total);
        var pendientes = porEstado.FirstOrDefault(x => x.Estado == OrderStatuses.Pendiente)?.Cantidad ?? 0;
        var entregados = porEstado.FirstOrDefault(x => x.Estado == OrderStatuses.Entregado)?.Cantidad ?? 0;

        var clientes = await _db.Clients.CountAsync();
        var vendedores = await _db.Sellers.CountAsync();

        var porMes = await _db.Orders
            .AsNoTracking()
            .GroupBy(p => new { p.Fecha.Year, p.Fecha.Month })
            .Select(g => new MesResumenDto { Year = g.Key.Year, Month = g.Key.Month, Cantidad = g.Count(), Ingresos = g.Sum(p => p.Total) })
            .OrderBy(x => x.Year).ThenBy(x => x.Month)
            .ToListAsync();

        var recientes = await _db.Orders.AsNoTracking()
            .OrderByDescending(p => p.Fecha)
            .Take(7)
            .Select(p => new PedidoRecienteDto
            {
                Id = p.Id,
                ClientName = p.Client != null ? p.Client.Nombre : "",
                SellerName = p.Seller != null ? p.Seller.Name : "",
                ProductName = p.ProductName,
                Total = p.Total,
                Fecha = p.Fecha,
                Estado = p.Estado
            }).ToListAsync();

        // KAN-24: serie mensual sin cancelados para los nuevos indicadores compactos.
        var serieMensual = await _db.Orders.AsNoTracking()
            .Where(p => p.Estado != OrderStatuses.Cancelado)
            .GroupBy(p => new { p.Fecha.Year, p.Fecha.Month })
            .Select(g => new SerieMensualDto
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                Pedidos = g.Count(),
                Ingresos = g.Sum(p => p.Total),
                Unidades = g.Sum(p => p.Cantidad)
            })
            .OrderBy(x => x.Year).ThenBy(x => x.Month)
            .ToListAsync();
        foreach (var mes in serieMensual)
            mes.TicketPromedio = mes.Pedidos == 0 ? 0 : mes.Ingresos / mes.Pedidos;

        // KAN-24: top 5 por unidades. Se agrupa también por nombre para no mezclar productos borrados.
        // El join se resuelve en dos consultas: EF no traduce un LeftJoin después de GroupBy con agregados.
        var topProductos = await _db.Orders.AsNoTracking()
            .Where(p => p.Estado != OrderStatuses.Cancelado)
            .GroupBy(p => new { p.ProductId, p.ProductName })
            .Select(g => new TopProductoDto
            {
                ProductId = g.Key.ProductId,
                ProductName = g.Key.ProductName,
                Unidades = g.Sum(p => p.Cantidad),
                Ingresos = g.Sum(p => p.Total)
            })
            .OrderByDescending(x => x.Unidades)
            .ThenByDescending(x => x.Ingresos)
            .ThenBy(x => x.ProductName)
            .Take(5)
            .ToListAsync();

        var idsConImagen = topProductos.Where(x => x.ProductId.HasValue).Select(x => x.ProductId!.Value).Distinct().ToList();
        var imagenes = await _db.Products.AsNoTracking()
            .Where(p => idsConImagen.Contains(p.Id))
            .Select(p => new { p.Id, p.ImageUrl })
            .ToDictionaryAsync(p => p.Id, p => p.ImageUrl);
        foreach (var producto in topProductos)
            producto.ImageUrl = producto.ProductId.HasValue && imagenes.TryGetValue(producto.ProductId.Value, out var url) ? url : null;

        return new DashboardMetricsDto
        {
            TotalPedidos = totalPedidos,
            Ingresos = ingresos,
            Pendientes = pendientes,
            Entregados = entregados,
            Clientes = clientes,
            Vendedores = vendedores,
            PorEstado = porEstado,
            PorMes = porMes,
            Recientes = recientes,
            SerieMensual = serieMensual,
            TopProductos = topProductos
        };
    }
}
