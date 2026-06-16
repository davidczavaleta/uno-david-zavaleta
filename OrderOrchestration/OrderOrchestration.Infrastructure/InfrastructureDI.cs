using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using OrderOrchestration.Application.Contracts;
using OrderOrchestration.Domain.Data;
using OrderOrchestration.Infrastructure.GrpcClients;
using OrderOrchestration.Infrastructure.Mongo;
using OrderOrchestration.Infrastructure.Outbox;
using OrderOrchestration.Infrastructure.Postgres;
using OrderOrchestration.Infrastructure.Resilience;
using OrderOrchestration.Infrastructure.Streaming;

namespace OrderOrchestration.Infrastructure
{
    public static class InfrastructureDI
    {
        /// <summary>
        /// Registra los servicios de infraestructura en el contenedor de dependencias.
        /// MongoDb
        /// MongoClient
        /// Postgres DbContext
        /// IOrderRepository
        /// IAuditRepository
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            //1. Mongo settings
            services.Configure<MongoDbSettings>(configuration.GetSection("MongoDbSettings"));

            //2. Mongo client 
            services.AddSingleton<IMongoClient>(sp =>
            {
                var settings = configuration.GetSection("MongoDbSettings").Get<MongoDbSettings>();
                return new MongoClient(settings.ConnectionString);
            });

            //3. Postgres db context
            services.AddDbContext<AuditDbContext>(options => options.UseNpgsql(configuration.GetConnectionString("AuditPostgres")));

            //4. Repositorios
            services.AddScoped<IOrderRepository, OrderRepository>();
            services.AddScoped<IAuditRepository, AuditRepository>();

            // 5. Pipeline de resiliencia de Polly (reutilizable)
            services.AddGrpcResiliencePipeline();

            // 6. Cliente gRPC tipado con la URL del servicio de fraude
            services.AddGrpcClient<FraudService.FraudServiceClient>(options =>
            {
                options.Address = new Uri(configuration["FraudService:Url"] ?? "https://localhost:5002");
            });

            // 7. Registrar la implementación del cliente de fraude
            services.AddScoped<IFraudCheckService, FraudCheckGrpcClient>();

            // 8. Notificador de estado en tiempo real (singleton para compartir suscripciones)
            services.AddSingleton<IOrderStatusNotifier, InMemoryOrderStatusNotifier>();

            // 9. Worker del patrón Outbox que publica los eventos de dominio de forma confiable
            services.AddHostedService<OutboxDispatcher>();

            return services;
        }
    }
}
