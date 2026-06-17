namespace OrderOrchestration.Domain
{
    /// <summary>
    /// El mensaje de la bandeja de salida (outbox) que representa un evento de dominio
    /// serializado en formato JSON, junto con su estado de procesamiento.
    /// </summary>
    public class OutboxMessage
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Type { get; set; } = string.Empty;  
        public string Content { get; set; } = string.Empty;
        public DateTime OccurredOn { get; set; } = DateTime.UtcNow;
        public bool Processed { get; set; }
        public DateTime? ProcessedOn { get; set; }
    }
}
