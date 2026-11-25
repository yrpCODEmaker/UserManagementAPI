using UserManagementAPI.Middleware;
using UserManagementAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// Registro de servicios en el contenedor de dependencias.
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddSingleton<IUserRepository, InMemoryUserRepository>();

var app = builder.Build();

// Configuración del pipeline de middleware corporativo en orden estricto:
// 1. Gestión global de excepciones
// 2. Autenticación basada en token Bearer
// 3. Registro de auditoría de solicitudes y respuestas
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<TokenAuthenticationMiddleware>();
app.UseMiddleware<RequestResponseLoggingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapGet("/api/test-error", (Func<IResult>)(() =>
        throw new InvalidOperationException("Excepción simulada para validar el middleware de gestión de errores.")));
}
else
{
    app.UseHttpsRedirection();
}

app.UseAuthorization();

app.MapControllers();

app.Run();
