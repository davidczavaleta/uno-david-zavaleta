namespace OrderOrchestration.Domain.Events
{
    /// <summary>
    /// Evento que se dispara cuando un operador resuelve manualmente una orden que estaba en
    /// estado <c>ManualReviewRequired</c> (human-in-the-loop), indicando si fue aprobada o rechazada
    /// y quién la resolvió.
    /// </summary>
    public class ManualReviewResolvedEvent : DomainEvent
    {
        public string OrderId { get; }
        public bool Approved { get; }
        public string Reviewer { get; }

        public ManualReviewResolvedEvent(string orderId, bool approved, string reviewer)
        {
            OrderId = orderId;
            Approved = approved;
            Reviewer = reviewer;
        }
    }
}
