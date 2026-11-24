using Microsoft.AspNetCore.Diagnostics;
using NeoOrders.Application.Contracts.Reportes;

namespace NeoOrders.Api.Infrastructure;

/// <summary>
/// Mapeo conservador de excepciones (KAN-39 A1): sólo las excepciones de dominio/control
/// explícitas se traducen a 4xx y se preservan sus shapes actuales. Las excepciones
/// genéricas (ArgumentException, InvalidOperationException, KeyNotFoundException, ...)
/// se dejan deliberadamente como 500: en este código se manejan en controllers/services
/// mediante Result/400 o se convierten en errores CSV, y mapearlas aquí cambiaría su
/// semántica y podría ocultar fallos reales de infraestructura.
/// </summary>
public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IHostEnvironment env) : IExceptionHandler
{
    private const string JsonContentType = "application/json; charset=utf-8";

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        // Única excepción de dominio/control explícita: contrato actual 400 { message }.
        // Es segura porque Paginacion.Crear es su única fuente y ya valida los límites.
        if (exception is PaginacionInvalidaException paginacionException)
        {
            httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            httpContext.Response.ContentType = JsonContentType;
            await httpContext.Response.WriteAsJsonAsync(new { message = paginacionException.Message }, cancellationToken);
            return true;
        }

        // Fallback sin cambios: 500 { message, traceId } y 'code' sólo en Development.
        logger.LogError(exception, "Unhandled exception");

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        httpContext.Response.ContentType = JsonContentType;

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
