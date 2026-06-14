namespace OrderOrchestration.Domain.Events
{
    /// <summary>
    /// Entidad base de la cual heredan todos los eventos de dominio, define un identificador único 
    /// y una marca de tiempo UTC para auditoria de cuándo ocurrió el evento.
    /// </summary>
    public class DomainEvent
    {
        public Guid EventId { get; set; }
        public DateTime OccurredOn { get; } = DateTime.UtcNow;
    }
}
