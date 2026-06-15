using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using OrderOrchestration.Domain.Data;
using OrderOrchestration.Infrastructure.Mongo;
using OrderOrchestration.Infrastructure.Postgres;

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
            services.AddSingleton<IMongoClient>(sp => {
                var settings = configuration.GetSection("MongoDbSettings").Get<MongoDbSettings>();
                return new MongoClient(settings.ConnectionString);
            });

            //3. Postgres db context
            services.AddDbContext<AuditDbContext>(options => options.UseNpgsql(configuration.GetConnectionString("AuditPostgres")));
                        
            //4. Repositorios
            services.AddScoped<IOrderRepository, OrderRepository>();
            services.AddScoped<IAuditRepository, AuditRepository>(); 

            return services;
        }
    }
}
