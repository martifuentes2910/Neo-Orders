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
}
