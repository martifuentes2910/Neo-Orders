namespace OrdersApi.Application.DTOs.Reportes;

/// <summary>Campos comunes de paginacion y discriminante para la union discriminada en TypeScript.</summary>
public abstract class DetalleResponseBase
{
    public string Tipo { get; set; } = string.Empty;
    public int TotalFilas { get; set; }
    public int Pagina { get; set; }
    public int TamanoPagina { get; set; }
    public int TotalPaginas { get; set; }
}
