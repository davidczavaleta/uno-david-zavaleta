using OrderOrchestration.Domain;
using OrderOrchestration.Domain.Data;

namespace OrderOrchestration.Infrastructure.Postgres
{
    public class AuditRepository : IAuditRepository
    {
        private readonly AuditDbContext _auditDbContext;

        public AuditRepository(AuditDbContext auditDbContext)
        {
            _auditDbContext = auditDbContext;
        }

        ///<inheritdoc />
        public Task AddLogAsync(AuditLog log)
        {
            _auditDbContext.AuditLogs.Add(log);
            return _auditDbContext.SaveChangesAsync();
        }
    }
}
