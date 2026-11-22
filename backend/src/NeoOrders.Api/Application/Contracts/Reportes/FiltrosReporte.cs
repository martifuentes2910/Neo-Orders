namespace OrdersApi.Application.Contracts.Reportes;

/// <summary>Filtros compartidos por los reportes de resumen y de detalle.</summary>
public sealed class FiltrosReporte
{
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }
    public int? ClientId { get; set; }
    public int? SellerId { get; set; }
    public string? Estado { get; set; }
}
