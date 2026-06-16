using MediatR;
using OrderOrchestration.Application.Contracts;
using OrderOrchestration.Domain;
using OrderOrchestration.Domain.Data;
using OrderOrchestration.Domain.Events;

namespace OrderOrchestration.Application.Orchestration
{
    /// <summary>
    /// Orquestador de órdenes que actúa como una máquina de estados ligera.
    /// Escucha todos los eventos de dominio y decide la siguiente transición de estado de la orden.
    /// </summary>
    public class Orchestrator :
    INotificationHandler<OrderSubmittedEvent>,
    INotificationHandler<FraudCheckedEvent>,
    INotificationHandler<OrderApprovedEvent>,
    INotificationHandler<OrderRejectedEvent>,
    INotificationHandler<PaymentProcessedEvent>,
    INotificationHandler<ShipmentRequestedEvent>
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IFraudCheckService _fraudCheckService;

        /// <summary>
        /// Inicializa el orquestador con el repositorio de órdenes requerido para leer y persistir el estado.
        /// </summary>
        public Orchestrator(IOrderRepository orderRepository, IFraudCheckService fraudCheckService)
        {
            _orderRepository = orderRepository;
            _fraudCheckService = fraudCheckService;
        }

        /// <summary>
        /// Reacciona a una orden recién enviada. Valida que esté en estado <c>Pending</c>
        /// y la transiciona a <c>FraudCheckPending</c> para iniciar la verificación de fraude.
        /// </summary>
        public async Task Handle(OrderSubmittedEvent notification, CancellationToken cancellationToken)
        {
            var order = await _orderRepository.GetByIdAsync(notification.OrderId);

            if (order == null || order.Status != OrderStatus.Pending)
                return;

            order.Status = OrderStatus.FraudCheckPending;

            var result = await _fraudCheckService.CheckFraudAsync(
                            order.OrderId.ToString(),
                            order.UserId,
                            order.TotalAmount,
                            cancellationToken);

            order.AddDomainEvent(new FraudCheckedEvent(
                order.OrderId.ToString(),
                result.IsApproved,
                result.Reason));

            await _orderRepository.SaveAsync(order);
        }

        /// <summary>
        /// Reacciona al resultado de la verificación de fraude. Si es aprobada transiciona a <c>Approved</c>
        /// y dispara <see cref="OrderApprovedEvent"/>. Si es rechazada, transiciona a <c>Rejected</c>.
        /// </summary>
        public async Task Handle(FraudCheckedEvent notification, CancellationToken cancellationToken)
        {
            var order = await _orderRepository.GetByIdAsync(notification.OrderId);

            if (order == null || order.Status != OrderStatus.FraudCheckPending)
                return;

            if (notification.IsApproved)
            {
                order.Status = OrderStatus.Approved;
                order.AddDomainEvent(new OrderApprovedEvent(order.OrderId.ToString()));
            }
            else
            {
                order.Status = OrderStatus.Rejected;
                order.AddDomainEvent(new OrderRejectedEvent(order.OrderId.ToString(), notification.Reason ?? string.Empty));
            }

            await _orderRepository.SaveAsync(order);
        }

        /// <summary>
        /// Reacciona a la aprobación de la orden. Simula el procesamiento del pago
        /// y transiciona el estado a <c>PaymentProcessed</c>.
        /// </summary>
        public async Task Handle(OrderApprovedEvent notification, CancellationToken cancellationToken)
        {
            var order = await _orderRepository.GetByIdAsync(notification.OrderId);

            if (order == null || order.Status != OrderStatus.Approved)
                return;

            order.Status = OrderStatus.PaymentProcessed;
            order.AddDomainEvent(new PaymentProcessedEvent(order.OrderId.ToString(), order.PaymentToken));

            await _orderRepository.SaveAsync(order);
        }

        /// <summary>
        /// Reacciona al procesamiento exitoso del pago. Transiciona el estado a <c>ShipmentRequested</c>
        /// y dispara <see cref="ShipmentRequestedEvent"/> para notificar al almacén.
        /// </summary>
        public async Task Handle(PaymentProcessedEvent notification, CancellationToken cancellationToken)
        {
            var order = await _orderRepository.GetByIdAsync(notification.OrderId);

            if (order == null || order.Status != OrderStatus.PaymentProcessed)
                return;

            order.Status = OrderStatus.ShipmentRequested;
            order.AddDomainEvent(new ShipmentRequestedEvent(order.OrderId.ToString()));

            await _orderRepository.SaveAsync(order);
        }

        /// <summary>
        /// Estado terminal del flujo de rechazo. La orden fue rechazada por fraude y el flujo se detiene aquí.
        /// </summary>
        public async Task Handle(OrderRejectedEvent notification, CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
        }

        /// <summary>
        /// Estado terminal del flujo feliz. La orden fue despachada exitosamente y el flujo concluye.
        /// </summary>
        public async Task Handle(ShipmentRequestedEvent notification, CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
        }
    }
}
