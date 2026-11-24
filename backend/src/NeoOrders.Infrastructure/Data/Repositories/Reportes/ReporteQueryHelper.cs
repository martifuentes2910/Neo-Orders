using NeoOrders.Application.Contracts.Reportes;
using NeoOrders.Application.DTOs.Reportes;
using NeoOrders.Domain.Orders;

namespace NeoOrders.Infrastructure.Data.Repositories.Reportes;

/// <summary>Filtros y exclusiones compartidos por todos los repositorios de reportes.</summary>
internal static class ReporteQueryHelper
{
    public static IQueryable<Order> AplicarFiltros(IQueryable<Order> query, FiltrosReporte filtros)
    {
        if (filtros.Desde.HasValue) query = query.Where(p => p.Fecha >= filtros.Desde.Value);
        if (filtros.Hasta.HasValue) query = query.Where(p => p.Fecha <= filtros.Hasta.Value);
        if (filtros.ClientId.HasValue) query = query.Where(p => p.ClientId == filtros.ClientId.Value);
        if (filtros.SellerId.HasValue) query = query.Where(p => p.SellerId == filtros.SellerId.Value);
        if (!string.IsNullOrWhiteSpace(filtros.Estado) && filtros.Estado != "Todos")
            query = query.Where(p => p.Estado == filtros.Estado);
        return query;
    }

    public static IQueryable<Order> ExcluirCancelados(IQueryable<Order> query) =>
        query.Where(p => p.Estado != OrderStatusPolicy.Cancelado);

    public static int CalcularTotalPaginas(int totalFilas, int tamanoPagina) =>
        totalFilas == 0 ? 0 : (int)Math.Ceiling((double)totalFilas / tamanoPagina);

    public static double CalcularParticipacion(decimal ingresos, decimal denominador) =>
        denominador == 0 ? 0 : (double)(ingresos / denominador * 100m);
}
