using MediatR;
using OrderOrchestration.Domain;
using OrderOrchestration.Domain.Data;
using OrderOrchestration.Domain.Events;

namespace OrderOrchestration.Application.Commands
{
    public class SubmitOrderCommandHandler : IRequestHandler<SubmitOrderCommand, Order>
    {
        private readonly IOrderRepository _orderRepository;
        public SubmitOrderCommandHandler(IOrderRepository orderRepository)
        {
            _orderRepository = orderRepository;
        }

        /// <summary>
        /// Event handler para procesar la orden de compra, implementa la lógica de negocio para:
        /// 1. Verificación de Idempotencia: Si el cliente reintenta la petición, el PaymentToken será el mismo. 
        /// 2. Mapear el command dto a la entidad de dominio.
        /// 3. Crear el evento de negocio y registrarlo en el outbox de la orden. 
        /// 4. Guardar orden (Estado + Outbox guardado atómicamente) 
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<Order> Handle(SubmitOrderCommand request, CancellationToken cancellationToken)
        {
            
            var existingOrder = await _orderRepository.GetByPaymentTokenAsync(request.PaymentToken);
            
            if (existingOrder != null)
            {
                return existingOrder;
            }

            var order = new Order
            {
                OrderId = request.OrderId,
                UserId = request.UserId,
                TotalAmount = request.TotalAmount,
                PaymentToken = request.PaymentToken,
                Status = OrderStatus.Pending,
                Items = request.Items.Select(i => new OrderItem
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice
                }).ToList()
            };

            var orderSubmittedEvent = new OrderSubmittedEvent(
                order.OrderId.ToString(),
                order.UserId,
                order.TotalAmount,
                order.Items,
                order.PaymentToken
            );
            order.AddDomainEvent(orderSubmittedEvent);

            await _orderRepository.SaveAsync(order);
            return order;
        }
    }
}
