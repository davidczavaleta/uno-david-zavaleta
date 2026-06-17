namespace OrderOrchestration.Domain.Data
{
    public interface IAuditRepository
    {
        /// <summary>
        /// Agrega un nuevo registro de auditoría a la base de datos.
        /// </summary>
        /// <param name="log"></param>
        /// <returns></returns>
        Task AddLogAsync(AuditLog log);
    }
}
