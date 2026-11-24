namespace NeoOrders.Domain.Sellers;

public partial class Seller
{
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
