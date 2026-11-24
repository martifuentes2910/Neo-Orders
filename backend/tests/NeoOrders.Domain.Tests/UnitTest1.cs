using NeoOrders.Domain.Orders;

namespace NeoOrders.Domain.Tests;

public class OrderStatusPolicyTests
{
    [Fact]
    public void CanTransition_AllowsConfiguredTransitions()
    {
        Assert.True(OrderStatusPolicy.CanTransition(OrderStatusPolicy.Pendiente, OrderStatusPolicy.Procesado));
        Assert.True(OrderStatusPolicy.CanTransition(OrderStatusPolicy.Procesado, OrderStatusPolicy.Enviado));
        Assert.True(OrderStatusPolicy.CanTransition(OrderStatusPolicy.Enviado, OrderStatusPolicy.Entregado));
        Assert.False(OrderStatusPolicy.CanTransition(OrderStatusPolicy.Pendiente, OrderStatusPolicy.Entregado));
        Assert.False(OrderStatusPolicy.CanTransition(OrderStatusPolicy.Cancelado, OrderStatusPolicy.Procesado));
    }

    [Fact]
    public void SameStatus_IsNoOp()
    {
        Assert.True(OrderStatusPolicy.CanTransition(OrderStatusPolicy.Pendiente, OrderStatusPolicy.Pendiente));
    }

    [Fact]
    public void Mapper_ParsesAndFormatsExternalStatus()
    {
        Assert.True(OrderStatusMapper.TryParse("pendiente", out var status));
        Assert.Equal(OrderStatus.Pendiente, status);
        Assert.Equal("Pendiente", status.ToExternal());
        Assert.Equal(OrderStatus.Enviado, OrderStatusMapper.FromExternal("Enviado"));
    }

    [Fact]
    public void IsValid_RejectsUnknownStatus()
    {
        Assert.True(OrderStatusPolicy.IsValid("Pendiente"));
        Assert.False(OrderStatusPolicy.IsValid("Invalido"));
        Assert.False(OrderStatusPolicy.IsValid(null));
    }
}
