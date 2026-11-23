namespace NeoOrders.Application.DTOs.Clients;

public class ClientListItemDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string? Direccion { get; set; }
    public DateTime FechaAlta { get; set; }
    public int OrdersCount { get; set; }
    public decimal TotalSpent { get; set; }
}

public class ClientDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string? Direccion { get; set; }
    public DateTime FechaAlta { get; set; }
}
