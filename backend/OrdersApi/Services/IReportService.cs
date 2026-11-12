namespace OrdersApi.Services;

public class FiltrosReporte
{
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }
    public int? ClientId { get; set; }
    public int? SellerId { get; set; }
    public string? Estado { get; set; }
}

public interface IReportService
{
    Task<object> GetDashboardMetricsAsync();
    Task<object> GetReportsAsync(FiltrosReporte filtros);
}
