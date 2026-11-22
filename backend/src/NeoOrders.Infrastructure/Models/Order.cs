using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NeoOrders.Domain.Orders;

namespace OrdersApi.Models;

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
