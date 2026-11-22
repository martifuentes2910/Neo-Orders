namespace NeoOrders.Domain.Orders;

public static class OrderStatuses
{
    public const string Pendiente = "Pendiente";
    public const string Procesado = "Procesado";
    public const string Enviado = "Enviado";
    public const string Entregado = "Entregado";
    public const string Cancelado = "Cancelado";

    public static readonly string[] All = [Pendiente, Procesado, Enviado, Entregado, Cancelado];
    public static readonly HashSet<string> EditLocked = [Procesado, Enviado, Entregado, Cancelado];
    public static readonly IReadOnlyDictionary<string, string[]> Transitions = new Dictionary<string, string[]>
    {
        [Pendiente] = [Procesado, Cancelado],
        [Procesado] = [Enviado, Cancelado],
        [Enviado] = [Entregado],
        [Entregado] = [],
        [Cancelado] = [],
    };

    public static bool CanTransition(string from, string to) =>
        from == to || (Transitions.TryGetValue(from, out var allowed) && allowed.Contains(to));
}
