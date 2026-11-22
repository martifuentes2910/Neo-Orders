namespace NeoOrders.Application.DTOs.Reportes;

public sealed class DashboardMetricsDto
{
    public int TotalPedidos { get; set; }
    public decimal Ingresos { get; set; }
    public int Pendientes { get; set; }
    public int Entregados { get; set; }
    public int Clientes { get; set; }
    public int Vendedores { get; set; }
    public List<EstadoResumenDto> PorEstado { get; set; } = new();
    public List<MesResumenDto> PorMes { get; set; } = new();
    public List<PedidoRecienteDto> Recientes { get; set; } = new();

    // KAN-24: campos nuevos, aditivos. Conservan el JSON histórico del panel.
    public List<SerieMensualDto> SerieMensual { get; set; } = new();
    public List<TopProductoDto> TopProductos { get; set; } = new();
}

/// <summary>Serie mensual del panel, sin pedidos cancelados (KAN-24).</summary>
public sealed class SerieMensualDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public int Pedidos { get; set; }
    public decimal Ingresos { get; set; }
    public int Unidades { get; set; }
    public decimal TicketPromedio { get; set; }
}

/// <summary>Top de productos por unidades vendidas, sin pedidos cancelados (KAN-24).</summary>
public sealed class TopProductoDto
{
    public int? ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public int Unidades { get; set; }
    public decimal Ingresos { get; set; }
}

public sealed class PedidoRecienteDto
{
    public int Id { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public string SellerName { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public DateTime Fecha { get; set; }
    public string Estado { get; set; } = string.Empty;
}
