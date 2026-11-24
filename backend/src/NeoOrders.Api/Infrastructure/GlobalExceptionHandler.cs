using Microsoft.AspNetCore.Diagnostics;
using NeoOrders.Application.Contracts.Reportes;

namespace NeoOrders.Api.Infrastructure;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IHostEnvironment env) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is PaginacionInvalidaException paginacionException)
        {
            httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            httpContext.Response.ContentType = "application/json; charset=utf-8";
            await httpContext.Response.WriteAsJsonAsync(new { message = paginacionException.Message }, cancellationToken);
            return true;
        }

        logger.LogError(exception, "Unhandled exception");

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        httpContext.Response.ContentType = "application/json; charset=utf-8";

        var payload = new Dictionary<string, object?>
        {
            ["message"] = env.IsDevelopment() ? exception.Message : "Ocurrió un error inesperado.",
            ["traceId"] = httpContext.TraceIdentifier
        };

        if (env.IsDevelopment())
        {
            payload["code"] = exception.GetType().Name;
        }

        await httpContext.Response.WriteAsJsonAsync(payload, cancellationToken);
        return true;
    }
}
