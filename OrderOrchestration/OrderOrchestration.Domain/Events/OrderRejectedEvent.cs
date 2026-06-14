namespace OrderOrchestration.Domain.Events
{
    /// <summary>
    /// Evento que se dispara cuando la orden no pudo se procesada, es decir, ha fallado la verificación de fraude o el pago ha sido rechazado.
    /// </summary>
    public class OrderRejectedEvent : DomainEvent
    {
        public string OrderId { get; }
        public string Reason { get; }

        public OrderRejectedEvent(string orderId, string reason)
        {
            OrderId = orderId;
            Reason = reason;
        }
    }
}
