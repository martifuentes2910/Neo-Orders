using System.Text.Json;
using NeoOrders.Application.DTOs.Import;

namespace NeoOrders.Application.Tests;

public class CsvImportContractTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public void CsvImportErrorDto_SerializesExpectedShape()
    {
        var dto = new CsvImportErrorDto { Row = 2, Field = "Email", Value = "bad", Message = "Email inválido" };
        var json = JsonSerializer.Serialize(dto, JsonOptions);
        using var doc = JsonDocument.Parse(json);

        Assert.Equal(2, doc.RootElement.GetProperty("row").GetInt32());
        Assert.Equal("Email", doc.RootElement.GetProperty("field").GetString());
        Assert.Equal("bad", doc.RootElement.GetProperty("value").GetString());
        Assert.Equal("Email inválido", doc.RootElement.GetProperty("message").GetString());
    }

    [Fact]
    public void CsvImportResultDto_SerializesExpectedShape()
    {
        var dto = new CsvImportResultDto
        {
            TotalRows = 3,
            ValidRows = 2,
            Imported = 2,
            Errors = [new CsvImportErrorDto { Row = 3, Field = "Sku", Value = "X", Message = "Error" }]
        };

        var json = JsonSerializer.Serialize(dto, JsonOptions);
        using var doc = JsonDocument.Parse(json);

        Assert.Equal(3, doc.RootElement.GetProperty("totalRows").GetInt32());
        Assert.Equal(2, doc.RootElement.GetProperty("validRows").GetInt32());
        Assert.Equal(2, doc.RootElement.GetProperty("imported").GetInt32());
        var errors = doc.RootElement.GetProperty("errors");
        Assert.Equal(JsonValueKind.Array, errors.ValueKind);
        Assert.Equal(1, errors.GetArrayLength());
        var error = errors[0];
        Assert.Equal(3, error.GetProperty("row").GetInt32());
        Assert.Equal("Sku", error.GetProperty("field").GetString());
        Assert.Equal("X", error.GetProperty("value").GetString());
        Assert.Equal("Error", error.GetProperty("message").GetString());
    }

    [Fact]
    public void CsvImportResultDto_EmptyErrors_SerializesAsEmptyArray()
    {
        var dto = new CsvImportResultDto { TotalRows = 1, ValidRows = 1, Imported = 1, Errors = [] };

        var json = JsonSerializer.Serialize(dto, JsonOptions);
        using var doc = JsonDocument.Parse(json);

        var errors = doc.RootElement.GetProperty("errors");
        Assert.Equal(JsonValueKind.Array, errors.ValueKind);
        Assert.Equal(0, errors.GetArrayLength());
    }
}
