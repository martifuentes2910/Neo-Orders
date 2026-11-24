using NeoOrders.Domain.Orders;

namespace NeoOrders.Domain.Tests;

public class OrderStatusesTests
{
    [Fact]
    public void CanTransition_AllowsConfiguredTransitions()
    {
        Assert.True(OrderStatuses.CanTransition(OrderStatuses.Pendiente, OrderStatuses.Procesado));
        Assert.True(OrderStatuses.CanTransition(OrderStatuses.Procesado, OrderStatuses.Enviado));
        Assert.True(OrderStatuses.CanTransition(OrderStatuses.Enviado, OrderStatuses.Entregado));
        Assert.False(OrderStatuses.CanTransition(OrderStatuses.Pendiente, OrderStatuses.Entregado));
        Assert.False(OrderStatuses.CanTransition(OrderStatuses.Cancelado, OrderStatuses.Procesado));
    }

    [Fact]
    public void SameStatus_IsNoOp()
    {
        Assert.True(OrderStatuses.CanTransition(OrderStatuses.Pendiente, OrderStatuses.Pendiente));
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
        Assert.True(OrderStatuses.IsValid("Pendiente"));
        Assert.False(OrderStatuses.IsValid("Invalido"));
        Assert.False(OrderStatuses.IsValid(null));
    }
}
