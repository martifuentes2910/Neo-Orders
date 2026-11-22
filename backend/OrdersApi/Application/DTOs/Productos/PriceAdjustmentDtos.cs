namespace OrdersApi.Application.DTOs.Productos;

public record PriceAdjustmentRequest
{
    public decimal Percent { get; init; }
}

public record PriceAdjustmentItemDto
{
    public int Id { get; init; }
    public string Sku { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public decimal CurrentBasePrice { get; init; }
    public decimal NewBasePrice { get; init; }
}

public record PriceAdjustmentPreviewResponse
{
    public decimal Percent { get; init; }
    public decimal Factor { get; init; }
    public int AffectedCount { get; init; }
    public int SkippedInactiveCount { get; init; }
    public IReadOnlyList<PriceAdjustmentItemDto> Items { get; init; } = [];
    public IReadOnlyList<string> Errors { get; init; } = [];
}

public record PriceAdjustmentApplyResponse
{
    public int AffectedCount { get; init; }
    public decimal Percent { get; init; }
    public decimal Factor { get; init; }
    public DateTime AppliedAtUtc { get; init; }
}