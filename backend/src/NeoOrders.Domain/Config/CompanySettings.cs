namespace NeoOrders.Domain.Config;

// KAN-38: definición persistible de la configuración de la empresa.
// El esquema se configura en Infrastructure/Data/Configurations/CompanySettingsConfiguration.
// Los setters son privados: el alta usa Create/Rehydrate y la edición UpdateDetails.
public class CompanySettings
{
    public int Id { get; private set; }
    public string NombreEmpresa { get; private set; } = "NeoOrders";
    public string Moneda { get; private set; } = "ARS";
    public decimal TipoDeCambio { get; private set; } = 1m;

    // Constructor para la materialización de EF; el resto del código usa las factories.
    private CompanySettings() { }

    public static CompanySettings Create(
        string nombreEmpresa = "NeoOrders",
        string moneda = "ARS",
        decimal tipoDeCambio = 1m) => Build(0, nombreEmpresa, moneda, tipoDeCambio);

    // Reconstrucción desde persistencia/fixtures: no revalida para no rechazar filas legacy.
    public static CompanySettings Rehydrate(
        int id,
        string nombreEmpresa = "NeoOrders",
        string moneda = "ARS",
        decimal tipoDeCambio = 1m) => Build(id, nombreEmpresa, moneda, tipoDeCambio);

    public void UpdateDetails(string nombreEmpresa, string moneda, decimal tipoDeCambio)
    {
        NombreEmpresa = nombreEmpresa;
        Moneda = moneda;
        TipoDeCambio = tipoDeCambio;
    }

    private static CompanySettings Build(int id, string nombreEmpresa, string moneda, decimal tipoDeCambio) => new()
    {
        Id = id,
        NombreEmpresa = nombreEmpresa,
        Moneda = moneda,
        TipoDeCambio = tipoDeCambio
    };
}
