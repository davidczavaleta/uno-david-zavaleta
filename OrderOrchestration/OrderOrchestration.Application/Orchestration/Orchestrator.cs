using System.Diagnostics;
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
    INotificationHandler<ManualReviewResolvedEvent>,
    INotificationHandler<OrderApprovedEvent>,
    INotificationHandler<OrderRejectedEvent>,
    INotificationHandler<PaymentProcessedEvent>,
    INotificationHandler<ShipmentRequestedEvent>
    {
        /// <summary>
        /// Fuente de trazas (OpenTelemetry) del orquestador. Debe registrarse en el pipeline de
        /// tracing con <c>AddSource(Orchestrator.ActivitySourceName)</c>.
        /// </summary>
        public const string ActivitySourceName = "OrderOrchestration.Orchestrator";
        private static readonly ActivitySource ActivitySource = new(ActivitySourceName);

        private readonly IOrderRepository _orderRepository;
        private readonly IFraudCheckService _fraudCheckService;
        private readonly IOrderStatusNotifier _statusNotifier;

        /// <summary>
        /// Inicializa el orquestador con el repositorio de órdenes requerido para leer y persistir el estado.
        /// </summary>
        public Orchestrator(
            IOrderRepository orderRepository,
            IFraudCheckService fraudCheckService,
            IOrderStatusNotifier statusNotifier)
        {
            _orderRepository = orderRepository;
            _fraudCheckService = fraudCheckService;
            _statusNotifier = statusNotifier;
        }

        /// <summary>
        /// Persiste la orden y notifica el cambio de estado a los suscriptores del stream de seguimiento.
        /// </summary>
        private async Task SaveAndNotifyAsync(Order order, CancellationToken cancellationToken)
        {
            await _orderRepository.SaveAsync(order);
            await _statusNotifier.PublishAsync(
                new OrderStatusUpdate(order.OrderId.ToString(), order.Status.ToString(), order.UpdatedAt),
                cancellationToken);
        }

        /// <summary>
        /// Reacciona a una orden recién enviada. Valida que esté en estado <c>Pending</c>
        /// y la transiciona a <c>FraudCheckPending</c> para iniciar la verificación de fraude.
        /// </summary>
        public async Task Handle(OrderSubmittedEvent notification, CancellationToken cancellationToken)
        {
            using var activity = ActivitySource.StartActivity("Orchestrator.OrderSubmitted");
            activity?.SetTag("order.id", notification.OrderId);

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
                result.Decision,
                result.Reason));

            await SaveAndNotifyAsync(order, cancellationToken);
        }

        /// <summary>
        /// Reacciona al resultado de la verificación de fraude:
        /// aprobada -> <c>Approved</c>; rechazada -> <c>Rejected</c>;
        /// revisión manual -> <c>ManualReviewRequired</c> (el flujo se detiene a la espera de un operador).
        /// </summary>
        public async Task Handle(FraudCheckedEvent notification, CancellationToken cancellationToken)
        {
            using var activity = ActivitySource.StartActivity("Orchestrator.FraudChecked");
            activity?.SetTag("order.id", notification.OrderId);
            activity?.SetTag("fraud.decision", notification.Decision.ToString());

            var order = await _orderRepository.GetByIdAsync(notification.OrderId);

            if (order == null || order.Status != OrderStatus.FraudCheckPending)
                return;

            switch (notification.Decision)
            {
                case FraudDecision.Approved:
                    order.Status = OrderStatus.Approved;
                    order.AddDomainEvent(new OrderApprovedEvent(order.OrderId.ToString()));
                    break;

                case FraudDecision.ManualReview:
                    // Human-in-the-loop: la orden queda en espera de resolución manual; no se emite
                    // ningún evento de continuación hasta que un operador la apruebe o rechace.
                    order.Status = OrderStatus.ManualReviewRequired;
                    break;

                case FraudDecision.Rejected:
                default:
                    order.Status = OrderStatus.Rejected;
                    order.AddDomainEvent(new OrderRejectedEvent(order.OrderId.ToString(), notification.Reason ?? string.Empty));
                    break;
            }

            await SaveAndNotifyAsync(order, cancellationToken);
        }

        /// <summary>
        /// Reacciona a la resolución manual de una orden en revisión. Si el operador la aprueba,
        /// transiciona a <c>Approved</c> y continúa el flujo; si la rechaza, transiciona a <c>Rejected</c>.
        /// </summary>
        public async Task Handle(ManualReviewResolvedEvent notification, CancellationToken cancellationToken)
        {
            using var activity = ActivitySource.StartActivity("Orchestrator.ManualReviewResolved");
            activity?.SetTag("order.id", notification.OrderId);
            activity?.SetTag("review.approved", notification.Approved);

            var order = await _orderRepository.GetByIdAsync(notification.OrderId);

            if (order == null || order.Status != OrderStatus.ManualReviewRequired)
                return;

            if (notification.Approved)
            {
                order.Status = OrderStatus.Approved;
                order.AddDomainEvent(new OrderApprovedEvent(order.OrderId.ToString()));
            }
            else
            {
                order.Status = OrderStatus.Rejected;
                order.AddDomainEvent(new OrderRejectedEvent(order.OrderId.ToString(), $"Rechazada manualmente por {notification.Reviewer}."));
            }

            await SaveAndNotifyAsync(order, cancellationToken);
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

            await SaveAndNotifyAsync(order, cancellationToken);
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

            await SaveAndNotifyAsync(order, cancellationToken);
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
