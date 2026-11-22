namespace NeoOrders.Application.DTOs.Import;

public sealed class CsvImportErrorDto
{
    public int Row { get; set; }
    public string Field { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public sealed class CsvImportResultDto
{
    public int TotalRows { get; set; }
    public int ValidRows { get; set; }
    public int Imported { get; set; }
    public List<CsvImportErrorDto> Errors { get; set; } = [];
}
