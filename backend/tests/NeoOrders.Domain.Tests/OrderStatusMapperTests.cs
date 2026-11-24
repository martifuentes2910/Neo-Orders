using NeoOrders.Domain.Orders;

namespace NeoOrders.Domain.Tests;

public class OrderStatusMapperTests
{
    [Fact]
    public void ToExternal_ReturnsCanonicalName()
    {
        Assert.Equal("Pendiente", OrderStatus.Pendiente.ToExternal());
    }

    [Fact]
    public void FromExternal_Invalid_ThrowsArgumentOutOfRange()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => OrderStatusMapper.FromExternal("Malo"));
        Assert.Contains("Estado no válido", ex.Message);
    }
}
