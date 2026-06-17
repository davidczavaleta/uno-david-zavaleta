namespace OrderOrchestration.Domain.Events
{
    /// <summary>
    /// Evento que se dispara cuando el servicio de fraude ha procesado la orden, contiene el resultado de la verificación
    /// (aprobada, rechazada o revisión manual) y el motivo asociado.
    /// </summary>
    public class FraudCheckedEvent : DomainEvent
    {
        public string OrderId { get; }
        public FraudDecision Decision { get; }
        public string Reason { get; }

        public FraudCheckedEvent(string orderId, FraudDecision decision, string reason = "")
        {
            OrderId = orderId;
            Decision = decision;
            Reason = reason;
        }
    }
}
