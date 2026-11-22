namespace OrdersApi.Application.DTOs.Reportes;

public sealed class VentasVendedorResponse : DetalleResponseBase
{
    public VentasVendedorResponse() => Tipo = "vendedor";

    /// <summary>Serie completa filtrada, usada por el grafico.</summary>
    public List<FilaVendedorDto> FilasGrafico { get; set; } = new();

    /// <summary>Pagina actual, usada por la tabla.</summary>
    public List<FilaVendedorDto> Filas { get; set; } = new();
}
