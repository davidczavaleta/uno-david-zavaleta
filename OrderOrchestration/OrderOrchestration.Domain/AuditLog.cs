namespace OrderOrchestration.Domain
{
    /// <summary>
    /// Modelo de datos para representar un registro de auditoría de acciones realizadas en una orden. 
    /// Contiene el Id de la orden afectada, la acción realizada, detalles adicionales para structured loggin
    /// y la marca de tiempo de la acción.
    /// </summary>
    public class AuditLog
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string OrderId { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty; 
        public string Details { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
