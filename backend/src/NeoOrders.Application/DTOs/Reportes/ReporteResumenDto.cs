namespace OrdersApi.Application.DTOs.Reportes;

public sealed class ReporteResumenDto
{
    public List<EstadoResumenDto> PorEstado { get; set; } = new();
    public List<MesResumenDto> PorMes { get; set; } = new();
    public List<ProductoResumenDto> TopProductos { get; set; } = new();
    public List<ClienteResumenDto> TopClientes { get; set; } = new();
    public List<VendedorResumenDto> VentasPorVendedor { get; set; } = new();
}

public sealed class EstadoResumenDto
{
    public string Estado { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public decimal Total { get; set; }
}

public sealed class MesResumenDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public int Cantidad { get; set; }
    public decimal Ingresos { get; set; }
}

public sealed class ProductoResumenDto
{
    public string Producto { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public decimal Ingresos { get; set; }
}

public sealed class ClienteResumenDto
{
    public int ClienteId { get; set; }
    public string Cliente { get; set; } = string.Empty;
    public int Pedidos { get; set; }
    public decimal Total { get; set; }
}

public sealed class VendedorResumenDto
{
    public int VendedorId { get; set; }
    public string Vendedor { get; set; } = string.Empty;
    public decimal Comision { get; set; }
    public int Pedidos { get; set; }
    public decimal Total { get; set; }
    public decimal ComisionEstimada { get; set; }
}
