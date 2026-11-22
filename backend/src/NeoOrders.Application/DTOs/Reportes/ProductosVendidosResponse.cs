namespace NeoOrders.Application.DTOs.Reportes;

public sealed class ProductosVendidosResponse : DetalleResponseBase
{
    public ProductosVendidosResponse() => Tipo = "producto";

    /// <summary>Serie completa filtrada, usada por el grafico.</summary>
    public List<FilaProductoDto> FilasGrafico { get; set; } = new();

    /// <summary>Pagina actual, usada por la tabla.</summary>
    public List<FilaProductoDto> Filas { get; set; } = new();
}
