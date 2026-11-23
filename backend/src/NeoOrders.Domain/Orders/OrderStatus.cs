namespace NeoOrders.Domain.Orders;

public enum OrderStatus
{
    Pendiente = 0,
    Procesado = 1,
    Enviado = 2,
    Entregado = 3,
    Cancelado = 4,
}

public static class OrderStatusMapper
{
    public static bool TryParse(string? value, out OrderStatus status)
    {
        status = default;
        if (string.IsNullOrWhiteSpace(value)) return false;
        return Enum.TryParse(value.Trim(), ignoreCase: true, out status) && Enum.IsDefined(status);
    }

    public static string ToExternal(this OrderStatus status) => status.ToString();

    public static OrderStatus FromExternal(string value)
    {
        if (TryParse(value, out var status)) return status;
        throw new ArgumentOutOfRangeException(nameof(value), value, "Estado no válido");
    }
}
