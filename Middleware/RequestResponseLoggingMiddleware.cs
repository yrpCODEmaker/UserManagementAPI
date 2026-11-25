using System.Diagnostics;

namespace UserManagementAPI.Middleware;

/// <summary>
/// Middleware de auditoría que registra método HTTP, ruta y código de estado de cada solicitud.
/// </summary>
public sealed class RequestResponseLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestResponseLoggingMiddleware> _logger;

    public RequestResponseLoggingMiddleware(RequestDelegate next, ILogger<RequestResponseLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            await _next(context);
            sw.Stop();
            _logger.LogInformation(
                "Audit HTTP {Method} {Path} => {StatusCode} ({ElapsedMs}ms)",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                sw.ElapsedMilliseconds);
        }
        catch
        {
            sw.Stop();
            _logger.LogWarning(
                "Audit HTTP {Method} {Path} => 500 (unhandled exception, {ElapsedMs}ms)",
                context.Request.Method,
                context.Request.Path,
                sw.ElapsedMilliseconds);
            throw;
        }
    }
}
