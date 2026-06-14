namespace OrderOrchestration.Domain.Events
{
    /// <summary>
    /// Evento que se dispara cuando el servicio de envío ha recibido la solicitud de envío para una orden,
    /// contiene el ID de la orden para la cual se solicitó el envío.
    /// </summary>
    public class ShipmentRequestedEvent : DomainEvent
    {
        public string OrderId { get; }

        public ShipmentRequestedEvent(string orderId)
        {
            OrderId = orderId;
        }
    }
}
