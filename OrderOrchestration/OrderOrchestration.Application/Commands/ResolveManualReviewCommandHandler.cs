using MediatR;
using OrderOrchestration.Domain;
using OrderOrchestration.Domain.Data;
using OrderOrchestration.Domain.Events;

namespace OrderOrchestration.Application.Commands
{
    /// <summary>
    /// Procesa la resolución manual de una orden en revisión: valida que la orden exista y
    /// esté en <c>ManualReviewRequired</c>, registra el evento <see cref="ManualReviewResolvedEvent"/>
    /// en el outbox y persiste el cambio. La transición de estado la realiza el orquestador al
    /// consumir el evento.
    /// </summary>
    public class ResolveManualReviewCommandHandler : IRequestHandler<ResolveManualReviewCommand, Order?>
    {
        private readonly IOrderRepository _orderRepository;

        public ResolveManualReviewCommandHandler(IOrderRepository orderRepository)
        {
            _orderRepository = orderRepository;
        }

        public async Task<Order?> Handle(ResolveManualReviewCommand request, CancellationToken cancellationToken)
        {
            var order = await _orderRepository.GetByIdAsync(request.OrderId.ToString());

            if (order is null || order.Status != OrderStatus.ManualReviewRequired)
            {
                return null;
            }

            order.AddDomainEvent(new ManualReviewResolvedEvent(
                order.OrderId.ToString(),
                request.Approved,
                request.Reviewer));

            await _orderRepository.SaveAsync(order);
            return order;
        }
    }
}
