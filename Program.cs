using UserManagementAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// Registro de servicios en el contenedor de dependencias.
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddSingleton<IUserRepository, InMemoryUserRepository>();

var app = builder.Build();

// Configuración del pipeline de solicitudes HTTP.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
else
{
    app.UseHttpsRedirection();
}

app.UseAuthorization();

app.MapControllers();

app.Run();
