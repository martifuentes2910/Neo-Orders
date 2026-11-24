namespace NeoOrders.Domain.Config;

// KAN-38: definición persistible de la configuración de la empresa. Sólo propiedades;
// el comportamiento vive en CompanySettings.Behavior.cs. Sin dependencias de EF/AspNet/Identity/DataAnnotations.
// El esquema se configura en Infrastructure/Data/Configurations/CompanySettingsConfiguration.
// Los setters son privados: la creación/reconstrucción se hace con las factories
// Create/Rehydrate y las mutaciones con los métodos del dominio.
public partial class CompanySettings
{
    public int Id { get; private set; }
    public string NombreEmpresa { get; private set; } = "NeoOrders";
    public string Moneda { get; private set; } = "ARS";
    public decimal TipoDeCambio { get; private set; } = 1m;

    // Constructor para la materialización de EF; el resto del código usa las factories.
    private CompanySettings() { }
}
