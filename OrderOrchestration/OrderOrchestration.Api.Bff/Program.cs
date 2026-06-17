using OrderOrchestration.Api.Bff.Hubs;
using OrderOrchestration.Api.Bff.Services;
using OrderOrchestration.Api.Core;

var builder = WebApplication.CreateBuilder(args);

const string CorsPolicy = "AngularClient";

// REST + documentación OpenAPI
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// SignalR para el seguimiento de órdenes en tiempo real
builder.Services.AddSignalR();
builder.Services.AddSingleton<IOrderStatusRelay, OrderStatusRelay>();

// Cliente gRPC tipado hacia el servicio de órdenes (Api.Core), sobre HTTP/2.
builder.Services.AddGrpcClient<OrderService.OrderServiceClient>(options =>
{
    options.Address = new Uri(builder.Configuration["OrderService:Url"] ?? "http://localhost:5136");
});

// CORS para el cliente Angular
var allowedOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>()
    ?? new[] { "http://localhost:4200" };

builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicy, policy =>
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors(CorsPolicy);

app.MapControllers();
app.MapHub<OrderStatusHub>("/hubs/orders");

app.Run();
