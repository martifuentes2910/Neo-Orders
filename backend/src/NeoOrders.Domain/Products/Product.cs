namespace NeoOrders.Domain.Products;

// KAN-38: definición persistible del producto. Sólo propiedades y navegaciones;
// el comportamiento vive en Product.Behavior.cs. Sin dependencias de EF/AspNet/Identity/DataAnnotations.
// El esquema se configura en Infrastructure/Data/Configurations/ProductConfiguration.
// Los setters son privados: la creación/reconstrucción se hace con las factories
// Create/Rehydrate y las mutaciones con los métodos del dominio.
public partial class Product
{
    public int Id { get; private set; }
    public string Sku { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Category { get; private set; } = "General";
    public decimal BasePrice { get; private set; }
    public decimal Cost { get; private set; }
    public int Stock { get; private set; }
    public int MinStockAlert { get; private set; } = 5;
    public bool Active { get; private set; } = true;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    // KAN-23: imagen alojada en Cloudinary. El cliente sólo necesita la URL pública.
    public string? ImageUrl { get; private set; }

    // KAN-23: identificador interno para reemplazar o borrar el asset.
    public string? ImagePublicId { get; private set; }

    // Constructor para la materialización de EF; el resto del código usa las factories.
    private Product() { }
}
