using System.ComponentModel.DataAnnotations;

namespace OrdersApi.Models;

public class Client
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio")]
    [StringLength(100, MinimumLength = 2)]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El email es obligatorio")]
    [EmailAddress(ErrorMessage = "Email inválido")]
    public string Email { get; set; } = string.Empty;

    [StringLength(30)]
    public string? Telefono { get; set; }

    [StringLength(200)]
    public string? Direccion { get; set; }

    public DateTime FechaAlta { get; set; } = DateTime.UtcNow;

    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
