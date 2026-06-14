namespace OrderOrchestration.Domain
{
    /// <summary>
    /// Representacion de un registro en la lista de items de la orden principal.
    /// </summary>
    public class OrderItem
    {
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}
