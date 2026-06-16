namespace OrderOrchestration.Domain.Events
{
    /// <summary>
    /// Evento que se dispara cuando una orden es enviada, contiene toda la información 
    /// necesaria para que los servicios de pago y fraude puedan procesar la orden.
    /// </summary>
    public class OrderSubmittedEvent : DomainEvent
    {
        public string OrderId { get; }
        public string UserId { get; }
        public decimal TotalAmount { get; }
        public List<OrderItem> Items { get; }
        public string PaymentToken { get; }

        public OrderSubmittedEvent(string orderId, string userId, decimal totalAmount, List<OrderItem> items, string paymentToken)
        {
            OrderId = orderId;
            UserId = userId;
            TotalAmount = totalAmount;
            Items = items;
            PaymentToken = paymentToken;
        }
    }
}
