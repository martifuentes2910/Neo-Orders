namespace NeoOrders.Domain.Orders;

public static class OrderStatuses
{
    public const string Pendiente = nameof(OrderStatus.Pendiente);
    public const string Procesado = nameof(OrderStatus.Procesado);
    public const string Enviado = nameof(OrderStatus.Enviado);
    public const string Entregado = nameof(OrderStatus.Entregado);
    public const string Cancelado = nameof(OrderStatus.Cancelado);

    public static readonly string[] All = Enum.GetNames<OrderStatus>();
    public static readonly HashSet<string> EditLocked = [Procesado, Enviado, Entregado, Cancelado];
    public static readonly IReadOnlyDictionary<string, string[]> Transitions = new Dictionary<string, string[]>
    {
        [Pendiente] = [Procesado, Cancelado],
        [Procesado] = [Enviado, Cancelado],
        [Enviado] = [Entregado],
        [Entregado] = [],
        [Cancelado] = [],
    };

    public static bool IsValid(string? estado) => !string.IsNullOrWhiteSpace(estado) && All.Contains(estado);

    public static bool CanTransition(string from, string to) =>
        from == to || (Transitions.TryGetValue(from, out var allowed) && allowed.Contains(to));

    public static bool CanTransition(OrderStatus from, OrderStatus to) =>
        CanTransition(from.ToExternal(), to.ToExternal());

    // KAN-38: efectos de stock de las transiciones, regla pura de dominio.
    public static bool DecreasesStock(OrderStatus from, OrderStatus to) =>
        from == OrderStatus.Pendiente && to == OrderStatus.Procesado;

    public static bool RestoresStock(OrderStatus from, OrderStatus to) =>
        from == OrderStatus.Procesado && to == OrderStatus.Cancelado;
}
