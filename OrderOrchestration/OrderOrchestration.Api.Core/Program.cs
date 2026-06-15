using OrderOrchestration.Api.Core.Services;
using OrderOrchestration.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddGrpc();
builder.Services.AddGrpcHealthChecks();
builder.Services.AddGrpcReflection();

//Agrega los servicios personalizados de infraestructura
builder.Services.AddInfrastructureServices(builder.Configuration);

var app = builder.Build();

IWebHostEnvironment env = app.Environment;

//Mapping for testing...
if (env.IsDevelopment())
{
    app.MapGrpcReflectionService();
}

app.MapGrpcHealthChecksService();

// Configure the HTTP request pipeline.
app.MapGrpcService<GreeterService>();
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

app.Run();
