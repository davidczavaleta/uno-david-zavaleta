namespace OrderOrchestration.Domain.Events
{
    /// <summary>
    /// Evento que se dispara cuando el servicio de pago ha procesado la orden, contiene el token de pago utilizado para la transacción.
    /// </summary>
    public class PaymentProcessedEvent : DomainEvent
    {
        public string OrderId { get; }
        public string PaymentToken { get; }

        public PaymentProcessedEvent(string orderId, string paymentToken)
        {
            OrderId = orderId;
            PaymentToken = paymentToken;
        }
    }
}
