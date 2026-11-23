using NeoOrders.Application.Contracts.Reportes;

namespace NeoOrders.Application.Tests;

public class PaginacionTests
{
    [Fact]
    public void Crear_UsaDefaults()
    {
        var p = Paginacion.Crear(null, null);
        Assert.Equal(1, p.Pagina);
        Assert.Equal(Paginacion.TamanoPorDefecto, p.TamanoPagina);
    }

    [Fact]
    public void Crear_PageMenorA1_Lanza()
    {
        var ex = Assert.Throws<PaginacionInvalidaException>(() => Paginacion.Crear(0, 25));
        Assert.Contains("page", ex.Message);
    }

    [Fact]
    public void Crear_PageSizeFueraDeRango_Lanza()
    {
        var ex = Assert.Throws<PaginacionInvalidaException>(() => Paginacion.Crear(1, 101));
        Assert.Contains("pageSize", ex.Message);
    }

    [Fact]
    public void Crear_PageSizeCero_Lanza()
    {
        var ex = Assert.Throws<PaginacionInvalidaException>(() => Paginacion.Crear(1, 0));
        Assert.Contains("pageSize", ex.Message);
    }

    [Fact]
    public void Crear_PageSizeMaximo_EsValido()
    {
        var p = Paginacion.Crear(2, Paginacion.TamanoMaximo);
        Assert.Equal(2, p.Pagina);
        Assert.Equal(Paginacion.TamanoMaximo, p.TamanoPagina);
    }
}
