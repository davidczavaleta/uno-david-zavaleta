using MediatR;
using OrderOrchestration.Domain;
using OrderOrchestration.Domain.Data;
using OrderOrchestration.Domain.Events;

namespace OrderOrchestration.Application.Auditing
{
    /// <summary>
    /// Registra en el almacén de auditoría (SQL) cada paso del flujo de orquestación.
    /// Se mantiene separado del <c>Orchestrator</c> para respetar el principio de responsabilidad
    /// única: el orquestador decide transiciones de estado y este handler persiste la traza de auditoría.
    /// Nunca registra datos sensibles en claro (el <c>PaymentToken</c> se enmascara).
    /// </summary>
    public class AuditEventHandler :
        INotificationHandler<OrderSubmittedEvent>,
        INotificationHandler<FraudCheckedEvent>,
        INotificationHandler<ManualReviewResolvedEvent>,
        INotificationHandler<OrderApprovedEvent>,
        INotificationHandler<OrderRejectedEvent>,
        INotificationHandler<PaymentProcessedEvent>,
        INotificationHandler<ShipmentRequestedEvent>
    {
        private readonly IAuditRepository _auditRepository;

        public AuditEventHandler(IAuditRepository auditRepository)
        {
            _auditRepository = auditRepository;
        }

        public Task Handle(OrderSubmittedEvent notification, CancellationToken cancellationToken) =>
            WriteAsync(notification.OrderId, "OrderSubmitted",
                $"Usuario={notification.UserId}; Monto={notification.TotalAmount}; Items={notification.Items.Count}");

        public Task Handle(FraudCheckedEvent notification, CancellationToken cancellationToken) =>
            WriteAsync(notification.OrderId, "FraudChecked",
                $"Decision={notification.Decision}; Motivo={notification.Reason}");

        public Task Handle(ManualReviewResolvedEvent notification, CancellationToken cancellationToken) =>
            WriteAsync(notification.OrderId, "ManualReviewResolved",
                $"Aprobada={notification.Approved}; Operador={notification.Reviewer}");

        public Task Handle(OrderApprovedEvent notification, CancellationToken cancellationToken) =>
            WriteAsync(notification.OrderId, "OrderApproved", "Orden aprobada tras la verificación de fraude.");

        public Task Handle(OrderRejectedEvent notification, CancellationToken cancellationToken) =>
            WriteAsync(notification.OrderId, "OrderRejected", $"Motivo={notification.Reason}");

        public Task Handle(PaymentProcessedEvent notification, CancellationToken cancellationToken) =>
            WriteAsync(notification.OrderId, "PaymentProcessed",
                $"PaymentToken={MaskToken(notification.PaymentToken)}");

        public Task Handle(ShipmentRequestedEvent notification, CancellationToken cancellationToken) =>
            WriteAsync(notification.OrderId, "ShipmentRequested", "Solicitud de envío generada.");

        private Task WriteAsync(string orderId, string action, string details)
        {
            var log = new AuditLog
            {
                OrderId = orderId,
                Action = action,
                Details = details,
                Timestamp = DateTime.UtcNow
            };

            return _auditRepository.AddLogAsync(log);
        }

        /// <summary>
        /// Enmascara un token sensible dejando visibles solo los últimos 4 caracteres.
        /// </summary>
        private static string MaskToken(string token) =>
            string.IsNullOrEmpty(token) || token.Length <= 4
                ? "****"
                : "****" + token[^4..];
    }
}
