using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using NeoOrders.Api.Infrastructure;
using NeoOrders.Application.Contracts.Reportes;

namespace NeoOrders.Api.Tests;

public class GlobalExceptionHandlerTests
{
    [Fact]
    public async Task TryHandleAsync_PaginacionInvalida_Retorna400ConContratoMessage()
    {
        var handler = new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance, new TestHostEnvironment("Production"));
        var context = CreateHttpContext();
        var exception = new PaginacionInvalidaException("page debe ser mayor o igual a 1.");

        var handled = await handler.TryHandleAsync(context, exception, CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        Assert.StartsWith("application/json", context.Response.ContentType, StringComparison.OrdinalIgnoreCase);

        var body = ReadBody(context);
        Assert.Equal("page debe ser mayor o igual a 1.", body.GetProperty("message").GetString());
        Assert.False(body.TryGetProperty("code", out _));
        Assert.False(body.TryGetProperty("traceId", out _));
    }

    [Fact]
    public async Task TryHandleAsync_UnhandledEnDevelopment_Retorna500ConDetalle()
    {
        var handler = new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance, new TestHostEnvironment("Development"));
        var context = CreateHttpContext();
        var exception = new InvalidOperationException("detalle interno");

        var handled = await handler.TryHandleAsync(context, exception, CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        Assert.StartsWith("application/json", context.Response.ContentType, StringComparison.OrdinalIgnoreCase);

        var body = ReadBody(context);
        Assert.Equal("detalle interno", body.GetProperty("message").GetString());
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("traceId").GetString()));
        Assert.Equal(nameof(InvalidOperationException), body.GetProperty("code").GetString());
    }

    [Fact]
    public async Task TryHandleAsync_UnhandledFueraDeDevelopment_Retorna500SinDetalleTecnico()
    {
        var handler = new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance, new TestHostEnvironment("Production"));
        var context = CreateHttpContext();
        context.TraceIdentifier = "trace-test-123";
        var exception = new InvalidOperationException("detalle interno");

        var handled = await handler.TryHandleAsync(context, exception, CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);

        var body = ReadBody(context);
        var message = body.GetProperty("message").GetString();
        Assert.False(string.IsNullOrWhiteSpace(message));
        Assert.DoesNotContain("detalle interno", message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("trace-test-123", body.GetProperty("traceId").GetString());
        Assert.False(body.TryGetProperty("code", out _));
    }

    private static DefaultHttpContext CreateHttpContext()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.TraceIdentifier = "trace-default";
        return context;
    }

    private static JsonElement ReadBody(HttpContext context)
    {
        context.Response.Body.Position = 0;
        using var doc = JsonDocument.Parse(context.Response.Body);
        return doc.RootElement.Clone();
    }

    private sealed class TestHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "NeoOrders.Api.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
    }
}
