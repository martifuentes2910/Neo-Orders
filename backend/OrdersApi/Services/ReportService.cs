using Microsoft.EntityFrameworkCore;
using OrdersApi.Data;
using OrdersApi.Models;

namespace OrdersApi.Services;

public class ReportService : IReportService
{
    private readonly AppDbContext _db;
    public ReportService(AppDbContext db) => _db = db;

    public async Task<object> GetDashboardMetricsAsync()
    {
        var totalPedidos = await _db.Orders.CountAsync();
        var ingresos = await _db.Orders.Where(p => p.Estado != OrderStatuses.Cancelado).SumAsync(p => (decimal?)p.Total) ?? 0m;
        var pendientes = await _db.Orders.CountAsync(p => p.Estado == OrderStatuses.Pendiente);
        var entregados = await _db.Orders.CountAsync(p => p.Estado == OrderStatuses.Entregado);
        var clientes = await _db.Clients.CountAsync();
        var vendedores = await _db.Sellers.CountAsync();

        var porEstado = await _db.Orders
            .GroupBy(p => p.Estado)
            .Select(g => new { estado = g.Key, cantidad = g.Count() })
            .ToListAsync();

        var recientes = await _db.Orders.Include(p => p.Client).Include(p => p.Seller)
            .OrderByDescending(p => p.Fecha)
            .Take(5)
            .Select(p => new
            {
                p.Id,
                clientName = p.Client != null ? p.Client.Nombre : "",
                sellerName = p.Seller != null ? p.Seller.Name : "",
                productName = p.ProductName,
                p.Total,
                p.Fecha,
                p.Estado
            }).ToListAsync();

        return new { totalPedidos, ingresos, pendientes, entregados, clientes, vendedores, porEstado, recientes };
    }

    public async Task<object> GetReportsAsync(FiltrosReporte f)
    {
        var q = _db.Orders.AsNoTracking().AsQueryable();
        if (f.Desde.HasValue) q = q.Where(p => p.Fecha >= f.Desde.Value);
        if (f.Hasta.HasValue) q = q.Where(p => p.Fecha <= f.Hasta.Value);
        if (f.ClientId.HasValue) q = q.Where(p => p.ClientId == f.ClientId.Value);
        if (f.SellerId.HasValue) q = q.Where(p => p.SellerId == f.SellerId.Value);
        if (!string.IsNullOrWhiteSpace(f.Estado) && f.Estado != "Todos")
            q = q.Where(p => p.Estado == f.Estado);

        var porEstado = await q
            .GroupBy(p => p.Estado)
            .Select(g => new { estado = g.Key, cantidad = g.Count(), total = g.Sum(p => p.Total) })
            .ToListAsync();

        var porMes = await q
            .GroupBy(p => new { p.Fecha.Year, p.Fecha.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, cantidad = g.Count(), ingresos = g.Sum(p => p.Total) })
            .OrderBy(x => x.Year).ThenBy(x => x.Month)
            .ToListAsync();

        var topProductos = await q
            .Where(p => p.Estado != OrderStatuses.Cancelado)
            .GroupBy(p => p.ProductName)
            .Select(g => new { producto = g.Key, cantidad = g.Sum(p => p.Cantidad), ingresos = g.Sum(p => p.Total) })
            .OrderByDescending(x => x.ingresos)
            .Take(5)
            .ToListAsync();

        var topClientes = await q
            .Where(p => p.Estado != OrderStatuses.Cancelado)
            .GroupBy(p => new { p.ClientId, p.Client!.Nombre })
            .Select(g => new { clienteId = g.Key.ClientId, cliente = g.Key.Nombre, pedidos = g.Count(), total = g.Sum(p => p.Total) })
            .OrderByDescending(x => x.total)
            .Take(5)
            .ToListAsync();

        var ventasPorVendedor = await q
            .Where(p => p.SellerId != null && p.Estado != OrderStatuses.Cancelado)
            .GroupBy(p => new { p.SellerId, p.Seller!.Name, p.Seller.CommissionPercentage })
            .Select(g => new
            {
                vendedorId = g.Key.SellerId!.Value,
                vendedor = g.Key.Name,
                comision = g.Key.CommissionPercentage,
                pedidos = g.Count(),
                total = g.Sum(p => p.Total),
                comisionEstimada = g.Sum(p => p.Total) * (g.Key.CommissionPercentage / 100m)
            })
            .OrderByDescending(x => x.total)
            .ToListAsync();

        return new { porEstado, porMes, topProductos, topClientes, ventasPorVendedor };
    }
}
