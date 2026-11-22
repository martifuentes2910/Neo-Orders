using System.ComponentModel.DataAnnotations;

namespace OrdersApi.Models;

public class Notification
{
    public int Id { get; set; }

    /// <summary>cliente, pedido, producto, vendedor, usuario</summary>
    [Required]
    [StringLength(30)]
    public string Tipo { get; set; } = "sistema";

    /// <summary>creado, actualizado, eliminado</summary>
    [Required]
    [StringLength(30)]
    public string Accion { get; set; } = "actualizado";

    [Required]
    [StringLength(160)]
    public string Titulo { get; set; } = string.Empty;

    [Required]
    [StringLength(400)]
    public string Mensaje { get; set; } = string.Empty;

    [StringLength(160)]
    public string? Link { get; set; }

    [StringLength(120)]
    public string? Usuario { get; set; }

    public DateTime Fecha { get; set; } = DateTime.UtcNow;

    public int? EntidadId { get; set; }
}
