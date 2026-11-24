namespace NeoOrders.Domain.Products;

// KAN-38: definición persistible del producto. Sin DataAnnotations ni dependencias de EF.
// El esquema se configura en Infrastructure/Data/Configurations/ProductConfiguration.
// Los setters son privados: las altas usan Create/Rehydrate y las mutaciones los métodos del dominio.
public class Product
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

    public bool IsCriticalStock => Stock <= MinStockAlert;

    // KAN-23: imagen alojada en Cloudinary. El cliente sólo necesita la URL pública.
    public string? ImageUrl { get; private set; }

    // KAN-23: identificador interno para reemplazar o borrar el asset.
    public string? ImagePublicId { get; private set; }

    // Constructor para la materialización de EF; el resto del código usa las factories.
    private Product() { }

    public static Product Create(
        string sku,
        string name,
        string category = "General",
        decimal basePrice = 0m,
        decimal cost = 0m,
        int stock = 0,
        int minStockAlert = 5,
        bool active = true,
        DateTime? createdAt = null,
        string? imageUrl = null,
        string? imagePublicId = null) => Build(
            0, sku, name, category, basePrice, cost, stock, minStockAlert, active, createdAt, imageUrl, imagePublicId);

    // Reconstrucción desde persistencia/fixtures: no revalida para no rechazar filas legacy.
    public static Product Rehydrate(
        int id,
        string sku,
        string name,
        string category = "General",
        decimal basePrice = 0m,
        decimal cost = 0m,
        int stock = 0,
        int minStockAlert = 5,
        bool active = true,
        DateTime? createdAt = null,
        string? imageUrl = null,
        string? imagePublicId = null) => Build(
            id, sku, name, category, basePrice, cost, stock, minStockAlert, active, createdAt, imageUrl, imagePublicId);

    public void UpdateDetails(
        string sku,
        string name,
        string category,
        decimal basePrice,
        decimal cost,
        int stock,
        int minStockAlert,
        bool active)
    {
        Sku = sku;
        Name = name;
        Category = category;
        BasePrice = basePrice;
        Cost = cost;
        Stock = stock;
        MinStockAlert = minStockAlert;
        Active = active;
    }

    public void SetImage(string url, string publicId)
    {
        ImageUrl = url;
        ImagePublicId = publicId;
    }

    public void ClearImage()
    {
        ImageUrl = null;
        ImagePublicId = null;
    }

    public void SetBasePrice(decimal basePrice) => BasePrice = basePrice;

    // El efecto (descuento o reembolso) lo decide Order; aquí sólo se aplica el delta.
    public void AdjustStock(int delta) => Stock += delta;

    private static Product Build(
        int id,
        string sku,
        string name,
        string category,
        decimal basePrice,
        decimal cost,
        int stock,
        int minStockAlert,
        bool active,
        DateTime? createdAt,
        string? imageUrl,
        string? imagePublicId) => new()
        {
            Id = id,
            Sku = sku,
            Name = name,
            Category = category,
            BasePrice = basePrice,
            Cost = cost,
            Stock = stock,
            MinStockAlert = minStockAlert,
            Active = active,
            CreatedAt = createdAt ?? DateTime.UtcNow,
            ImageUrl = imageUrl,
            ImagePublicId = imagePublicId
        };
}
