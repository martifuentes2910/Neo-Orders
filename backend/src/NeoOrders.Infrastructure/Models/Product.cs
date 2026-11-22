using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace NeoOrders.Infrastructure.Models;

public class Product
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El código SKU es obligatorio")]
    [StringLength(50)]
    public string Sku { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre es obligatorio")]
    [StringLength(120)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "La categoría es obligatoria")]
    [StringLength(60)]
    public string Category { get; set; } = "General";

    [Column(TypeName = "decimal(18,2)")]
    [Range(0.01, 10000000)]
    public decimal BasePrice { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Cost { get; set; }

    [Range(0, 100000)]
    public int Stock { get; set; }

    [Range(0, 1000)]
    public int MinStockAlert { get; set; } = 5;

    public bool Active { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsCriticalStock => Stock <= MinStockAlert;

    // KAN-23: imagen alojada en Cloudinary. El cliente sólo necesita la URL pública.
    [StringLength(500)]
    public string? ImageUrl { get; set; }

    // KAN-23: identificador interno para reemplazar o borrar el asset; nunca se expone en la API.
    [JsonIgnore]
    [StringLength(200)]
    public string? ImagePublicId { get; set; }
}
