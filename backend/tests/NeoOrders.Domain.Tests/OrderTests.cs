using NeoOrders.Domain.Orders;

namespace NeoOrders.Domain.Tests;

// KAN-38: invariantes y reglas puras del modelo de dominio Order.
public class OrderTests
{
    private static Order Create(
        int clientId = 1,
        int? sellerId = null,
        int? productId = 1,
        string productName = "Producto",
        int cantidad = 2,
        decimal precioUnitario = 10.5m,
        string? orderNumber = "ORD-1") =>
        Order.CreateNew(clientId, sellerId, productId, productName, cantidad, precioUnitario,
            new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc), orderNumber);

    [Fact]
    public void CreateNew_InicializaPendienteYCalculaTotal()
    {
        var order = Create();

        Assert.Equal(OrderStatusPolicy.Pendiente, order.Estado);
        Assert.Equal(OrderStatus.Pendiente, order.Status);
        Assert.Equal(21m, order.Total);
        Assert.Equal(0, order.Id);
        Assert.Equal(1, order.ClientId);
        Assert.Equal(1, order.ProductId);
        Assert.Equal("Producto", order.ProductName);
        Assert.Equal(2, order.Cantidad);
        Assert.Equal(10.5m, order.PrecioUnitario);
        Assert.Equal("ORD-1", order.OrderNumber);
        Assert.False(order.IsEditLocked);
    }

    [Fact]
    public void CreateNew_OrderNumberEnBlancoSeNormalizaANull()
    {
        Assert.Null(Create(orderNumber: "   ").OrderNumber);
        Assert.Null(Create(orderNumber: null).OrderNumber);
        Assert.Equal("ORD-2", Create(orderNumber: "  ORD-2  ").OrderNumber);
    }

    [Theory]
    [InlineData(0, "Producto", 1, 10.0, null)]
    [InlineData(1, "", 1, 10.0, null)]
    [InlineData(1, "   ", 1, 10.0, null)]
    [InlineData(1, "Producto", 0, 10.0, null)]
    [InlineData(1, "Producto", 1, 0.0, null)]
    [InlineData(1, "Producto", 1, -1.0, null)]
    [InlineData(1, "Producto", 1, 10.0, "123456789012345678901234567890123456789012345678901")]
    public void CreateNew_RechazaInvariantesInvalidos(
        int clientId, string productName, int cantidad, decimal precioUnitario, string? orderNumber)
    {
        Assert.ThrowsAny<ArgumentException>(() => Create(
            clientId: clientId, productName: productName, cantidad: cantidad,
            precioUnitario: precioUnitario, orderNumber: orderNumber));
    }

    [Fact]
    public void Rehydrate_ConservaEstadoSinRevalidar()
    {
        var order = Order.Rehydrate(7, 1, null, 1, "Prod", 3, 4m, 12m,
            DateTime.UtcNow, OrderStatusPolicy.Cancelado, "ORD-7");

        Assert.Equal(7, order.Id);
        Assert.Equal(OrderStatusPolicy.Cancelado, order.Estado);
        Assert.Equal(12m, order.Total);
        Assert.True(order.IsEditLocked);
    }

    [Theory]
    [InlineData(OrderStatus.Pendiente, false)]
    [InlineData(OrderStatus.Procesado, true)]
    [InlineData(OrderStatus.Enviado, true)]
    [InlineData(OrderStatus.Entregado, true)]
    [InlineData(OrderStatus.Cancelado, true)]
    public void IsEditLocked_SoloPendienteEsEditable(OrderStatus status, bool locked)
    {
        var order = CreateForStatus(status);

        Assert.Equal(locked, order.IsEditLocked);
    }

    [Theory]
    [InlineData(OrderStatus.Pendiente, OrderStatus.Procesado, true)]
    [InlineData(OrderStatus.Pendiente, OrderStatus.Cancelado, true)]
    [InlineData(OrderStatus.Pendiente, OrderStatus.Entregado, false)]
    [InlineData(OrderStatus.Procesado, OrderStatus.Enviado, true)]
    [InlineData(OrderStatus.Enviado, OrderStatus.Entregado, true)]
    [InlineData(OrderStatus.Entregado, OrderStatus.Pendiente, false)]
    public void CanTransitionTo_AplicaMatrizDeTransiciones(OrderStatus from, OrderStatus to, bool expected)
    {
        var order = CreateForStatus(from);

        Assert.Equal(expected, order.CanTransitionTo(to));
        Assert.Equal(expected, order.CanTransitionTo(to.ToExternal()));
    }

    [Fact]
    public void PlanStatusChange_PendienteAProcesado_DisminuyeStock()
    {
        var order = Create();

        var change = order.PlanStatusChange(OrderStatusPolicy.Procesado);

        Assert.True(change.Success);
        Assert.Null(change.Error);
        Assert.Equal(OrderStatus.Pendiente, change.From);
        Assert.Equal(OrderStatus.Procesado, change.To);
        Assert.Equal(OrderStockEffect.Decrease, change.StockEffect);
        Assert.False(change.IsNoOp);
        Assert.Equal(OrderStatusPolicy.Procesado, change.NewStatus);
    }

    [Fact]
    public void PlanStatusChange_ProcesadoACancelado_RestauraStock()
    {
        var order = CreateForStatus(OrderStatus.Procesado);

        var change = order.PlanStatusChange(OrderStatusPolicy.Cancelado);

        Assert.True(change.Success);
        Assert.Equal(OrderStockEffect.Restore, change.StockEffect);
    }

    [Fact]
    public void PlanStatusChange_PendienteACancelado_NoAfectaStock()
    {
        var order = Create();

        var change = order.PlanStatusChange(OrderStatusPolicy.Cancelado);

        Assert.True(change.Success);
        Assert.Equal(OrderStockEffect.None, change.StockEffect);
    }

    [Fact]
    public void PlanStatusChange_MismoEstado_ExitoSinEfecto()
    {
        var order = Create();

        var change = order.PlanStatusChange(OrderStatusPolicy.Pendiente);

        Assert.True(change.Success);
        Assert.True(change.IsNoOp);
        Assert.Equal(OrderStockEffect.None, change.StockEffect);
    }

    [Fact]
    public void PlanStatusChange_TransicionInvalida_DevuelveMensajeExterno()
    {
        var order = Create();

        var change = order.PlanStatusChange(OrderStatusPolicy.Entregado);

        Assert.False(change.Success);
        Assert.Equal("Transición de estado no permitida: Pendiente → Entregado", change.Error);
        Assert.Equal(OrderStockEffect.None, change.StockEffect);
    }

    [Fact]
    public void PlanStatusChange_EstadoInvalido_Falla()
    {
        var order = Create();

        var change = order.PlanStatusChange("NoExiste");

        Assert.False(change.Success);
        Assert.Equal("Estado no válido", change.Error);
    }

    [Fact]
    public void ApplyStatusChange_ActualizaEstadoExterno()
    {
        var order = Create();
        var change = order.PlanStatusChange(OrderStatusPolicy.Procesado);

        order.ApplyStatusChange(change);

        Assert.Equal(OrderStatusPolicy.Procesado, order.Estado);
        Assert.Equal(OrderStatus.Procesado, order.Status);
    }

    [Fact]
    public void ApplyStatusChange_ConCambioFallido_Lanza()
    {
        var order = Create();
        var change = order.PlanStatusChange(OrderStatusPolicy.Entregado);

        Assert.Throws<InvalidOperationException>(() => order.ApplyStatusChange(change));
        Assert.Equal(OrderStatusPolicy.Pendiente, order.Estado);
    }

    [Fact]
    public void UpdateDetails_ActualizaCamposYRecalculaTotal()
    {
        var order = Create();

        order.UpdateDetails(9, 4, 3);

        Assert.Equal(9, order.ClientId);
        Assert.Equal(4, order.SellerId);
        Assert.Equal(3, order.Cantidad);
        Assert.Equal(31.5m, order.Total);
    }

    [Fact]
    public void UpdateDetails_EstadoBloqueado_Lanza()
    {
        var order = CreateForStatus(OrderStatus.Procesado);

        Assert.Throws<InvalidOperationException>(() => order.UpdateDetails(9, 4, 3));
        Assert.Equal(1, order.ClientId);
        Assert.Equal(2, order.Cantidad);
    }

    [Fact]
    public void UpdateDetails_DatosInvalidos_Lanza()
    {
        var order = Create();

        Assert.Throws<ArgumentOutOfRangeException>(() => order.UpdateDetails(0, null, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => order.UpdateDetails(1, null, 0));
    }

    private static Order CreateForStatus(OrderStatus status)
    {
        var order = Create();
        order.ApplyStatusChange(new OrderStatusChange(true, null, OrderStatus.Pendiente, status, OrderStockEffect.None));
        return order;
    }
}
