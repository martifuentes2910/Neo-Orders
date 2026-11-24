using NeoOrders.Domain.Orders;

namespace NeoOrders.Domain.Sellers;

// KAN-38: definición persistible del vendedor. Sin DataAnnotations ni dependencias de EF.
// El esquema se configura en Infrastructure/Data/Configurations/SellerConfiguration.
public class Seller
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public decimal CommissionPercentage { get; set; } = 5.0m;
    public bool Active { get; set; } = true;

    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
