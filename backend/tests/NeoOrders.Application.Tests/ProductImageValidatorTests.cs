using NeoOrders.Application.Services;

namespace NeoOrders.Application.Tests;

public class ProductImageValidatorTests
{
    private static byte[] Png() => [137,80,78,71,13,10,26,10,0,0,0,0];

    [Fact]
    public void Validate_RejectsEmpty()
    {
        var err = ProductImageValidator.Validate("a.png", "image/png", []);
        Assert.Equal("Debe seleccionar una imagen.", err);
    }

    [Fact]
    public void Validate_RejectsUnsupportedExtension()
    {
        var err = ProductImageValidator.Validate("a.gif", "image/gif", Png());
        Assert.Contains("Formato no admitido", err);
    }

    [Fact]
    public void Validate_AcceptsValidPng()
    {
        var err = ProductImageValidator.Validate("a.png", "image/png", Png());
        Assert.Null(err);
    }
}
