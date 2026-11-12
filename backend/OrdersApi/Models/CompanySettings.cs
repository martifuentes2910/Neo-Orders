using System.ComponentModel.DataAnnotations;

namespace OrdersApi.Models;

public class CompanySettings
{
    public int Id { get; set; }

    [Required]
    [StringLength(120)]
    public string NombreEmpresa { get; set; } = "NeoOrders";

    [Required]
    [StringLength(10)]
    public string Moneda { get; set; } = "ARS";

    [Range(0.0001, 1000000)]
    public decimal TipoDeCambio { get; set; } = 1m;
}
