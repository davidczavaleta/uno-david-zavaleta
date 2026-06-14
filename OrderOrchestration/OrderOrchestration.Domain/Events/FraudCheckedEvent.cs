namespace OrderOrchestration.Domain.Events
{
    /// <summary>
    /// Evento que se dispara cuando el servicio de fraude ha procesado la orden, contiene el resultado de la verificación
    /// </summary>
    public class FraudCheckedEvent : DomainEvent
    {
        public string OrderId { get; }
        public bool IsApproved { get; }
        public string Reason { get; } 

        public FraudCheckedEvent(string orderId, bool isApproved, string reason = "")
        {
            OrderId = orderId;
            IsApproved = isApproved;
            Reason = reason;
        }
    }
}
