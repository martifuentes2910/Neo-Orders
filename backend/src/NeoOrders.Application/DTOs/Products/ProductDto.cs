namespace NeoOrders.Application.DTOs.Products;

public class ProductDto
{
    public int Id { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = "General";
    public decimal BasePrice { get; set; }
    public decimal Cost { get; set; }
    public int Stock { get; set; }
    public int MinStockAlert { get; set; } = 5;
    public bool Active { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public bool IsCriticalStock { get; set; }
    public string? ImageUrl { get; set; }
}
