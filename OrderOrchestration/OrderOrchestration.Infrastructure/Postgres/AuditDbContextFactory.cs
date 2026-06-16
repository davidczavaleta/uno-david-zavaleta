using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace OrderOrchestration.Infrastructure.Postgres
{
    /// <summary>
    /// Fábrica usada únicamente en tiempo de diseño (por las herramientas de EF Core, p. ej.
    /// <c>dotnet ef migrations</c>). Permite crear el <see cref="AuditDbContext"/> sin necesidad
    /// de levantar el host de la aplicación. La cadena de conexión es solo para generar migraciones,
    /// no se utiliza en tiempo de ejecución.
    /// </summary>
    public class AuditDbContextFactory : IDesignTimeDbContextFactory<AuditDbContext>
    {
        public AuditDbContext CreateDbContext(string[] args)
        {
            var connectionString = Environment.GetEnvironmentVariable("AUDIT_POSTGRES_CONNECTION")
                ?? "Host=localhost;Port=5432;Database=AuditDb;Username=postgres;Password=pgadmin";

            var optionsBuilder = new DbContextOptionsBuilder<AuditDbContext>();
            optionsBuilder.UseNpgsql(connectionString);

            return new AuditDbContext(optionsBuilder.Options);
        }
    }
}
