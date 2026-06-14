using OrderOrchestration.Domain.Events;
using System.Text.Json;

namespace OrderOrchestration.Domain
{
    /// <summary>
    /// Entidad principal del dominio que representa una orden de compra.
    /// Ademas de sus propiedades básicas, contiene una lista de mensajes de la bandeja de salida (outbox)
    /// y encapsula el método para agregar eventos de dominio a dicha lista.
    /// </summary>
    public class Order
    {
        public Guid OrderId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public List<OrderItem> Items { get; set; } = new();
        public decimal TotalAmount { get; set; }

        // El token de idempotency 
        public string PaymentToken { get; set; } = string.Empty;

        public OrderStatus Status { get; set; } = OrderStatus.Pending;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Lista de mensajes de la bandeja de salida.
        /// </summary>
         public List<OutboxMessage> OutboxMessages { get; set; } = new();

        /// <summary>
        /// Registra un evento de dominio en la bandeja de salida (outbox) 
        /// para que luego sea procesado por el worker y enviado al bus de mensajes. 
        /// </summary>
        /// <param name="domainEvent"></param>
        public void AddDomainEvent(DomainEvent domainEvent)
        {
            var outboxMessage = new OutboxMessage
            {
                Id = domainEvent.EventId,
                Type = domainEvent.GetType().AssemblyQualifiedName ?? domainEvent.GetType().Name,
                Content = JsonSerializer.Serialize(domainEvent, domainEvent.GetType()),
                OccurredOn = domainEvent.OccurredOn,
                Processed = false
            };
            OutboxMessages.Add(outboxMessage);
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
