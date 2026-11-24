using NeoOrders.Domain.Orders;

namespace NeoOrders.Domain.Tests;

// KAN-38: bloquea los strings externos canónicos y las reglas puras de dominio.
public class OrderStatusContractTests
{
    [Fact]
    public void All_ExponeLosEstadosExternosEnOrdenCanonico()
    {
        Assert.Equal(
            new[] { "Pendiente", "Procesado", "Enviado", "Entregado", "Cancelado" },
            OrderStatusPolicy.All);
    }

    [Fact]
    public void Mapper_RoundTrip_ConservaTodosLosEstados()
    {
        Assert.All(OrderStatusPolicy.All, external =>
            Assert.Equal(external, OrderStatusMapper.FromExternal(external).ToExternal()));
    }

    [Theory]
    [InlineData(OrderStatus.Pendiente, OrderStatus.Procesado, true, false)]
    [InlineData(OrderStatus.Procesado, OrderStatus.Cancelado, false, true)]
    [InlineData(OrderStatus.Pendiente, OrderStatus.Cancelado, false, false)]
    [InlineData(OrderStatus.Procesado, OrderStatus.Enviado, false, false)]
    [InlineData(OrderStatus.Enviado, OrderStatus.Entregado, false, false)]
    public void StockRules_SoloAplicanALasTransicionesPrevistas(
        OrderStatus from, OrderStatus to, bool decreases, bool restores)
    {
        Assert.Equal(decreases, OrderStatusPolicy.DecreasesStock(from, to));
        Assert.Equal(restores, OrderStatusPolicy.RestoresStock(from, to));
    }
}
