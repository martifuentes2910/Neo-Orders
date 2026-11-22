namespace OrdersApi.Application.DTOs.Reportes;

public sealed class FilaClienteDto
{
    public int ClienteId { get; set; }
    public string Cliente { get; set; } = string.Empty;
    public int Pedidos { get; set; }
    public int Unidades { get; set; }
    public decimal Ingresos { get; set; }
    public double Participacion { get; set; }
}
