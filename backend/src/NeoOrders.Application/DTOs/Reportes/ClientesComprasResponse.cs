namespace OrdersApi.Application.DTOs.Reportes;

public sealed class ClientesComprasResponse : DetalleResponseBase
{
    public ClientesComprasResponse() => Tipo = "cliente";

    /// <summary>Serie completa filtrada, usada por el grafico.</summary>
    public List<FilaClienteDto> FilasGrafico { get; set; } = new();

    /// <summary>Pagina actual, usada por la tabla.</summary>
    public List<FilaClienteDto> Filas { get; set; } = new();
}
