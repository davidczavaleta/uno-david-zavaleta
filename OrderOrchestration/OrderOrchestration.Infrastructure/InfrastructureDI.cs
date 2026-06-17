using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using OrderOrchestration.Domain;
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
            // El driver MongoDB 3.x cambia GuidRepresentation a Unspecified por defecto.
            // Hay que registrar el serializador globalmente para TODOS los GUIDs (ej. OrderItem.ProductId).
            BsonSerializer.TryRegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));

            // Configurar BsonClassMap para Order y OutboxMessage:
            // - MapId: usa OrderId como _id en Mongo (evita ObjectId auto-generado duplicado)
            // - SetIgnoreExtraElements: tolera campos no mapeados (compatibilidad driver 3.x)
            // - GuidSerializer(Standard): serializa Guids como UUID RFC 4122
            // Se registra aquí (Infrastructure) para no contaminar el Domain con refs a MongoDB.
            if (!BsonClassMap.IsClassMapRegistered(typeof(Order)))
            {
                BsonClassMap.RegisterClassMap<Order>(cm =>
                {
                    cm.AutoMap();
                    cm.SetIgnoreExtraElements(true);
                    cm.MapIdMember(c => c.OrderId)
                      .SetSerializer(new GuidSerializer(GuidRepresentation.Standard));
                });
            }

            if (!BsonClassMap.IsClassMapRegistered(typeof(OutboxMessage)))
            {
                BsonClassMap.RegisterClassMap<OutboxMessage>(cm =>
                {
                    cm.AutoMap();
                    cm.SetIgnoreExtraElements(true);
                    cm.MapMember(c => c.Id)
                      .SetSerializer(new GuidSerializer(GuidRepresentation.Standard));
                });
            }

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
