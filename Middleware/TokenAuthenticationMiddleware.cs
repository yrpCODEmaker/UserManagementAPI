using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace UserManagementAPI.Middleware;

/// <summary>
/// Middleware de autenticación basada en token Bearer configurado en appsettings.json.
/// </summary>
public sealed class TokenAuthenticationMiddleware
{
    private const string BearerPrefix = "Bearer ";
    private static readonly string UnauthorizedPayload = JsonSerializer.Serialize(new
    {
        error = "Unauthorized. Missing or invalid token."
    });

    private readonly RequestDelegate _next;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<TokenAuthenticationMiddleware> _logger;
    private readonly IReadOnlyList<byte[]> _validTokenBytes;

    public TokenAuthenticationMiddleware(
        RequestDelegate next,
        IConfiguration configuration,
        IHostEnvironment environment,
        ILogger<TokenAuthenticationMiddleware> logger)
    {
        _next = next;
        _environment = environment;
        _logger = logger;

        var configuredTokens = configuration.GetSection("Authentication:ValidTokens").Get<string[]>() ?? Array.Empty<string>();
        _validTokenBytes = configuredTokens
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => Encoding.UTF8.GetBytes(t.Trim()))
            .ToArray();
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (_environment.IsDevelopment() &&
            context.Request.Path.StartsWithSegments("/openapi", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        if (!TryExtractBearerToken(context.Request, out var token) || !IsTokenValid(token))
        {
            _logger.LogWarning(
                "HTTP {Method} {Path} rejected with 401 Unauthorized.",
                context.Request.Method,
                context.Request.Path);

            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.Headers.WWWAuthenticate = "Bearer";
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(UnauthorizedPayload);
            return;
        }

        await _next(context);
    }

    private static bool TryExtractBearerToken(HttpRequest request, out string token)
    {
        token = string.Empty;
        if (!request.Headers.TryGetValue("Authorization", out var headerValues))
        {
            return false;
        }

        var rawHeader = headerValues.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(rawHeader) ||
            !rawHeader.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        token = rawHeader[BearerPrefix.Length..].Trim();
        return !string.IsNullOrEmpty(token);
    }

    private bool IsTokenValid(string token)
    {
        if (_validTokenBytes.Count == 0)
        {
            return false;
        }

        var candidateBytes = Encoding.UTF8.GetBytes(token);
        var matched = false;
        foreach (var validBytes in _validTokenBytes)
        {
            if (CryptographicOperations.FixedTimeEquals(candidateBytes, validBytes))
            {
                matched = true;
            }
        }

        return matched;
    }
}
