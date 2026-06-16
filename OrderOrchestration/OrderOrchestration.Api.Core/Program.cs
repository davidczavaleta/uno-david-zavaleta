using Microsoft.EntityFrameworkCore;
using OrderOrchestration.Api.Core.Services;
using OrderOrchestration.Application.Commands;
using OrderOrchestration.Infrastructure;
using OrderOrchestration.Infrastructure.Postgres;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddGrpc();
builder.Services.AddGrpcHealthChecks();
builder.Services.AddGrpcReflection();

// MediatR: registra comandos, handlers y el orquestador (notification handlers) del ensamblado de Application.
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(SubmitOrderCommand).Assembly));

//Agrega los servicios personalizados de infraestructura
builder.Services.AddInfrastructureServices(builder.Configuration);

var app = builder.Build();

// Aplica las migraciones pendientes del store de auditoría (SQL) al arrancar.
// En entornos productivos (K8s) esto lo realizaría un init container; aquí se ejecuta
// de forma tolerante a fallos para no impedir el arranque si la base aún no está disponible.
using (var scope = app.Services.CreateScope())
{
    try
    {
        var auditDbContext = scope.ServiceProvider.GetRequiredService<AuditDbContext>();
        auditDbContext.Database.Migrate();
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogWarning(ex, "No se pudieron aplicar las migraciones de auditoría al arranque.");
    }
}

IWebHostEnvironment env = app.Environment;

//Mapping for testing...
if (env.IsDevelopment())
{
    app.MapGrpcReflectionService();
}

app.MapGrpcHealthChecksService();

// Configure the HTTP request pipeline.
app.MapGrpcService<OrderGrpcService>();
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

app.Run();
