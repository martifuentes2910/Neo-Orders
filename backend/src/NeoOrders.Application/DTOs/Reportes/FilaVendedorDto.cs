namespace NeoOrders.Application.DTOs.Reportes;

public sealed class FilaVendedorDto
{
    public int VendedorId { get; set; }
    public string Vendedor { get; set; } = string.Empty;
    public int Pedidos { get; set; }
    public int Unidades { get; set; }
    public decimal Ingresos { get; set; }
    public double Participacion { get; set; }
    public decimal Comision { get; set; }
    public decimal ComisionEstimada { get; set; }
}
