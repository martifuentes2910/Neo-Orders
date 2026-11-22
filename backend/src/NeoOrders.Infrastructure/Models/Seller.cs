using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NeoOrders.Infrastructure.Models;

public class Seller
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre del vendedor es obligatorio")]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [EmailAddress]
    [StringLength(100)]
    public string Email { get; set; } = string.Empty;

    [StringLength(30)]
    public string Phone { get; set; } = string.Empty;

    [Range(0, 100)]
    [Column(TypeName = "decimal(5,2)")]
    public decimal CommissionPercentage { get; set; } = 5.0m;

    public bool Active { get; set; } = true;

    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
