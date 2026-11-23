namespace NeoOrders.Application.DTOs.Sellers;

public class SellerDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public decimal CommissionPercentage { get; set; } = 5.0m;
    public bool Active { get; set; } = true;
}
