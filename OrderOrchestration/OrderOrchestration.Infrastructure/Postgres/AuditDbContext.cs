using Microsoft.EntityFrameworkCore;
using OrderOrchestration.Domain;

namespace OrderOrchestration.Infrastructure.Postgres
{
    public class AuditDbContext : DbContext
    {
        public AuditDbContext(DbContextOptions<AuditDbContext> options) : base(options)
        {
        }

        public DbSet<AuditLog> AuditLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.HasKey(e => e.Id);                
                entity.HasIndex(e => e.OrderId);
            });
        }
    }
}
