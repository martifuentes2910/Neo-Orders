namespace NeoOrders.Application.DTOs.Config;

public class ClearDataResultDto
{
    public string Message { get; set; } = "Datos operativos eliminados";
    public ClearDataCountsDto Eliminados { get; set; } = new();
}

public class ClearDataCountsDto
{
    public int Notificaciones { get; set; }
    public int Pedidos { get; set; }
    public int Productos { get; set; }
    public int Vendedores { get; set; }
    public int Clientes { get; set; }
}
