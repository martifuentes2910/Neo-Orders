namespace NeoOrders.Domain.Products;

// KAN-38: definición persistible del producto. Sin DataAnnotations ni dependencias de EF.
// El esquema se configura en Infrastructure/Data/Configurations/ProductConfiguration.
public class Product
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
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsCriticalStock => Stock <= MinStockAlert;

    // KAN-23: imagen alojada en Cloudinary. El cliente sólo necesita la URL pública.
    public string? ImageUrl { get; set; }

    // KAN-23: identificador interno para reemplazar o borrar el asset.
    public string? ImagePublicId { get; set; }
}
