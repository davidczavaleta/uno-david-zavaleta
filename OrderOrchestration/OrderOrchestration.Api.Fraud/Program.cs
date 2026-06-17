using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OrderOrchestration.Api.Fraud.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddGrpc();
builder.Services.AddHealthChecks().AddCheck("self", () => HealthCheckResult.Healthy());
builder.Services.AddGrpcHealthChecks();
builder.Services.AddGrpcReflection();

// Observabilidad: trazas distribuidas exportadas por OTLP (Jaeger).
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("OrderOrchestration.Api.Fraud"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter());

var app = builder.Build();

IHostEnvironment environment = app.Environment;
if (environment.IsDevelopment())
{
    app.MapGrpcReflectionService();
}

app.MapGrpcHealthChecksService();

// Configure the HTTP request pipeline.
app.MapGrpcService<FraudCheckService>();

app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

app.Run();
