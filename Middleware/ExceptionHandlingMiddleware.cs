using System.Text.Json;

namespace UserManagementAPI.Middleware;

/// <summary>
/// Middleware global para capturar excepciones no controladas y emitir respuestas JSON uniformes.
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private static readonly string InternalErrorPayload = JsonSerializer.Serialize(new { error = "Internal server error." });

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Excepción no controlada al procesar {Method} {Path}.",
                context.Request.Method,
                context.Request.Path);

            if (!context.Response.HasStarted)
            {
                context.Response.Clear();
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(InternalErrorPayload);
            }
        }
    }
}
