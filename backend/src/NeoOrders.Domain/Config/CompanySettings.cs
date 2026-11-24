namespace NeoOrders.Domain.Config;

// KAN-38: definición persistible de la configuración de la empresa.
// El esquema se configura en Infrastructure/Data/Configurations/CompanySettingsConfiguration.
public class CompanySettings
{
    public int Id { get; set; }
    public string NombreEmpresa { get; set; } = "NeoOrders";
    public string Moneda { get; set; } = "ARS";
    public decimal TipoDeCambio { get; set; } = 1m;
}
