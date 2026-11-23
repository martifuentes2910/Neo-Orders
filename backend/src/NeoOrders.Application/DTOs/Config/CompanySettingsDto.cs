namespace NeoOrders.Application.DTOs.Config;

public class CompanySettingsDto
{
    public int Id { get; set; }
    public string NombreEmpresa { get; set; } = "NeoOrders";
    public string Moneda { get; set; } = "ARS";
    public decimal TipoDeCambio { get; set; } = 1200m;
}
