namespace NeoOrders.Application.Contracts.Reportes;

/// <summary>Paginacion validada en servidor: 1-based, 25 por defecto, maximo 100.</summary>
public sealed class Paginacion
{
    public const int TamanoPorDefecto = 25;
    public const int TamanoMaximo = 100;

    public int Pagina { get; }
    public int TamanoPagina { get; }

    private Paginacion(int pagina, int tamanoPagina)
    {
        Pagina = pagina;
        TamanoPagina = tamanoPagina;
    }

    public static Paginacion Crear(int? pagina, int? tamanoPagina)
    {
        var p = pagina ?? 1;
        var t = tamanoPagina ?? TamanoPorDefecto;
        if (p < 1)
            throw new PaginacionInvalidaException("page debe ser mayor o igual a 1.");
        if (t < 1 || t > TamanoMaximo)
            throw new PaginacionInvalidaException($"pageSize debe estar entre 1 y {TamanoMaximo}.");
        return new Paginacion(p, t);
    }
}

public sealed class PaginacionInvalidaException : Exception
{
    public PaginacionInvalidaException(string message) : base(message) { }
}
