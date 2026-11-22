namespace OrdersApi.Services;

public class OrderResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public int? OrderId { get; set; }
    public string? ProductName { get; set; }
    public bool NotFound { get; set; }
}
