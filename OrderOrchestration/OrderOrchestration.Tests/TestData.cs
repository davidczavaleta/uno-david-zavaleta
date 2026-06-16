using OrderOrchestration.Domain;

namespace OrderOrchestration.Tests
{
    /// <summary>
    /// Fábrica de datos de prueba reutilizable para construir órdenes en estados específicos.
    /// </summary>
    internal static class TestData
    {
        public static Order CreateOrder(OrderStatus status, Guid? orderId = null, string paymentToken = "tok_123456789")
        {
            return new Order
            {
                OrderId = orderId ?? Guid.NewGuid(),
                UserId = "user-1",
                TotalAmount = 100m,
                PaymentToken = paymentToken,
                Status = status,
                Items = new List<OrderItem>
                {
                    new() { ProductId = Guid.NewGuid(), Quantity = 1, UnitPrice = 100m }
                }
            };
        }
    }
}
