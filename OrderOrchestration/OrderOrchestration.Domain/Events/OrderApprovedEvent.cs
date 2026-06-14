namespace OrderOrchestration.Domain.Events
{
    /// <summary>
    /// Evento que se dispara cuando la orden ha sido aprobada, es decir, ha pasado la verificación de fraude y el pago ha sido exitoso.
    /// </summary>
    public class OrderApprovedEvent : DomainEvent
    {
        public string OrderId { get; }

        public OrderApprovedEvent(string orderId)
        {
            OrderId = orderId;
        }
    }
}
