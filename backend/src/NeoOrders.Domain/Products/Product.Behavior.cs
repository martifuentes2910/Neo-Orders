namespace NeoOrders.Domain.Products;

public partial class Product
{
    // Propiedad calculada de dominio: se agrupa con el comportamiento, como en Order.
    public bool IsCriticalStock => Stock <= MinStockAlert;

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
