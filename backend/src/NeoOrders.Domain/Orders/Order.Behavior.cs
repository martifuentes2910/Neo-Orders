namespace NeoOrders.Domain.Orders;

public sealed partial class Order
{
    public OrderStatus Status => OrderStatusMapper.FromExternal(Estado);

    public bool IsEditLocked => OrderStatusPolicy.EditLocked.Contains(Estado);

    public static Order CreateNew(
        int clientId,
        int? sellerId,
        int? productId,
        string productName,
        int cantidad,
        decimal precioUnitario,
        DateTime fecha,
        string? orderNumber)
    {
        Validate(clientId, productName, cantidad, precioUnitario, orderNumber);

        return new Order
        {
            ClientId = clientId,
            SellerId = sellerId,
            ProductId = productId,
            ProductName = productName,
            Cantidad = cantidad,
            PrecioUnitario = precioUnitario,
            Total = CalculateTotal(cantidad, precioUnitario),
            Fecha = fecha,
            Estado = OrderStatusPolicy.Pendiente,
            OrderNumber = NormalizeOrderNumber(orderNumber)
        };
    }

    // Reconstrucción desde persistencia: no revalida para no rechazar filas legacy.
    public static Order Rehydrate(
        int id,
        int clientId,
        int? sellerId,
        int? productId,
        string productName,
        int cantidad,
        decimal precioUnitario,
        decimal total,
        DateTime fecha,
        string estado,
        string? orderNumber) => new()
        {
            Id = id,
            ClientId = clientId,
            SellerId = sellerId,
            ProductId = productId,
            ProductName = productName,
            Cantidad = cantidad,
            PrecioUnitario = precioUnitario,
            Total = total,
            Fecha = fecha,
            Estado = estado,
            OrderNumber = orderNumber
        };

    public void UpdateDetails(int clientId, int? sellerId, int cantidad)
    {
        if (IsEditLocked)
            throw new InvalidOperationException($"El pedido está {Estado}: no se puede editar, solo cambiar el estado");
        if (clientId <= 0)
            throw new ArgumentOutOfRangeException(nameof(clientId), clientId, "ClientId inválido");
        if (cantidad < 1)
            throw new ArgumentOutOfRangeException(nameof(cantidad), cantidad, "Cantidad inválida");

        ClientId = clientId;
        SellerId = sellerId;
        Cantidad = cantidad;
        Total = CalculateTotal(Cantidad, PrecioUnitario);
    }

    public bool CanTransitionTo(OrderStatus to) =>
        OrderStatusMapper.TryParse(Estado, out var from) && OrderStatusPolicy.CanTransition(from, to);

    // Coincide con la validación externa actual: strings canónicos, case-sensitive.
    public bool CanTransitionTo(string? to) => OrderStatusPolicy.CanTransition(Estado, to ?? string.Empty);

    public OrderStatusChange PlanStatusChange(string? newStatusRaw)
    {
        if (!OrderStatusMapper.TryParse(newStatusRaw, out var to))
            return OrderStatusChange.Failed("Estado no válido");
        if (!OrderStatusMapper.TryParse(Estado, out var from))
            return OrderStatusChange.Failed("Estado actual no válido");
        if (!OrderStatusPolicy.CanTransition(from, to))
            return OrderStatusChange.Failed($"Transición de estado no permitida: {Estado} → {to.ToExternal()}");

        var stockEffect = from == to ? OrderStockEffect.None
            : OrderStatusPolicy.DecreasesStock(from, to) ? OrderStockEffect.Decrease
            : OrderStatusPolicy.RestoresStock(from, to) ? OrderStockEffect.Restore
            : OrderStockEffect.None;

        return new OrderStatusChange(true, null, from, to, stockEffect);
    }

    public void ApplyStatusChange(OrderStatusChange change)
    {
        if (!change.Success)
            throw new InvalidOperationException(change.Error ?? "Transición de estado no válida");

        Estado = change.NewStatus;
    }

    private static decimal CalculateTotal(int cantidad, decimal precioUnitario) => cantidad * precioUnitario;

    private static string? NormalizeOrderNumber(string? orderNumber) =>
        string.IsNullOrWhiteSpace(orderNumber) ? null : orderNumber.Trim();

    private static void Validate(int clientId, string productName, int cantidad, decimal precioUnitario, string? orderNumber)
    {
        if (clientId <= 0)
            throw new ArgumentOutOfRangeException(nameof(clientId), clientId, "ClientId inválido");
        if (string.IsNullOrWhiteSpace(productName))
            throw new ArgumentException("El nombre del producto es obligatorio", nameof(productName));
        if (cantidad < 1)
            throw new ArgumentOutOfRangeException(nameof(cantidad), cantidad, "Cantidad inválida");
        if (precioUnitario <= 0)
            throw new ArgumentOutOfRangeException(nameof(precioUnitario), precioUnitario, "Precio unitario inválido");
        if (orderNumber != null && orderNumber.Length > 50)
            throw new ArgumentOutOfRangeException(nameof(orderNumber), orderNumber, "OrderNumber no puede superar 50 caracteres");
    }
}

public enum OrderStockEffect
{
    None,
    Decrease,
    Restore,
}

// Resultado puro de una transición: Infrastructure sólo aplica el efecto de stock indicado.
public sealed record OrderStatusChange(
    bool Success,
    string? Error,
    OrderStatus From,
    OrderStatus To,
    OrderStockEffect StockEffect)
{
    public bool IsNoOp => From == To;
    public string NewStatus => To.ToExternal();

    public static OrderStatusChange Failed(string error) =>
        new(false, error, default, default, OrderStockEffect.None);
}
