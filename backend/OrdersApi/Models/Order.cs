using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OrdersApi.Models;

public static class OrderStatuses
{
    public const string Pendiente = "Pendiente";
    public const string Procesado = "Procesado";
    public const string Enviado = "Enviado";
    public const string Entregado = "Entregado";
    public const string Cancelado = "Cancelado";

    public static readonly string[] All = { Pendiente, Procesado, Enviado, Entregado, Cancelado };

    // KAN-16/KAN-35: estados con datos fijos; sólo se permite cambiar el estado por PATCH.
    public static readonly HashSet<string> EditLocked = new() { Procesado, Enviado, Entregado, Cancelado };

    // KAN-16/KAN-35: transiciones de negocio. El mismo estado se acepta como no-op.
    public static readonly IReadOnlyDictionary<string, string[]> Transitions =
        new Dictionary<string, string[]>
        {
            [Pendiente] = new[] { Procesado, Cancelado },
            [Procesado] = new[] { Enviado, Cancelado },
            [Enviado] = new[] { Entregado },
            [Entregado] = Array.Empty<string>(),
            [Cancelado] = Array.Empty<string>(),
        };

    public static bool CanTransition(string from, string to) =>
        from == to || (Transitions.TryGetValue(from, out var allowed) && allowed.Contains(to));
}

public class Order
{
    public int Id { get; set; }

    [Required]
    public int ClientId { get; set; }
    public Client? Client { get; set; }

    public int? SellerId { get; set; }
    public Seller? Seller { get; set; }

    public int? ProductId { get; set; }
    public Product? Product { get; set; }

    [Required(ErrorMessage = "El nombre del producto es obligatorio")]
    [StringLength(120)]
    public string ProductName { get; set; } = string.Empty;

    [Range(1, 100000, ErrorMessage = "La cantidad debe ser mayor a 0")]
    public int Cantidad { get; set; } = 1;

    [Range(0.01, 10000000, ErrorMessage = "El precio unitario debe ser mayor a 0")]
    [Column(TypeName = "decimal(18,2)")]
    public decimal PrecioUnitario { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Total { get; set; }

    public DateTime Fecha { get; set; } = DateTime.UtcNow;

    [Required]
    [StringLength(30)]
    public string Estado { get; set; } = OrderStatuses.Pendiente;

    // KAN-26: clave externa opcional para importación CSV / idempotencia operativa.
    [StringLength(50)]
    public string? OrderNumber { get; set; }
}
