using NeoOrders.Domain.Orders;

namespace NeoOrders.Domain.Sellers;

// KAN-38: definición persistible del vendedor. Sin DataAnnotations ni dependencias de EF.
// El esquema se configura en Infrastructure/Data/Configurations/SellerConfiguration.
// Los setters son privados: las altas usan Create/Rehydrate y las ediciones UpdateDetails.
public class Seller
{
    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string Phone { get; private set; } = string.Empty;
    public decimal CommissionPercentage { get; private set; } = 5.0m;
    public bool Active { get; private set; } = true;

    // Navegación requerida por el mapeo EF (OrderConfiguration).
    public ICollection<Order> Orders { get; private set; } = new List<Order>();

    // Constructor para la materialización de EF; el resto del código usa las factories.
    private Seller() { }

    public static Seller Create(
        string name,
        string email,
        string? phone = null,
        decimal? commissionPercentage = null,
        bool active = true) => Build(0, name, email, phone, commissionPercentage, active);

    // Reconstrucción desde persistencia/fixtures: no revalida para no rechazar filas legacy.
    public static Seller Rehydrate(
        int id,
        string name,
        string email,
        string? phone = null,
        decimal? commissionPercentage = null,
        bool active = true) => Build(id, name, email, phone, commissionPercentage, active);

    public void UpdateDetails(string name, string email, string phone, decimal commissionPercentage, bool active)
    {
        Name = name;
        Email = email;
        Phone = phone;
        CommissionPercentage = commissionPercentage;
        Active = active;
    }

    private static Seller Build(
        int id,
        string name,
        string email,
        string? phone,
        decimal? commissionPercentage,
        bool active) => new()
        {
            Id = id,
            Name = name,
            Email = email,
            Phone = phone ?? string.Empty,
            CommissionPercentage = commissionPercentage ?? 5.0m,
            Active = active
        };
}
