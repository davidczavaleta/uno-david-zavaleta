using Grpc.Core;
using MediatR;
using OrderOrchestration.Application.Commands;
using OrderOrchestration.Application.Contracts;
using OrderOrchestration.Domain;
using OrderOrchestration.Domain.Data;

namespace OrderOrchestration.Api.Core.Services
{
    /// <summary>
    /// Punto de entrada gRPC del sistema. Traduce las peticiones gRPC a comandos de
    /// aplicacion (MediatR) y expone el seguimiento de estado en tiempo real via streaming.
    /// Su unica responsabilidad es el manejo del protocolo; la logica de negocio vive en los handlers.
    /// </summary>
    public class OrderGrpcService : OrderService.OrderServiceBase
    {
        private readonly ISender _mediator;
        private readonly IOrderRepository _orderRepository;
        private readonly IOrderStatusNotifier _statusNotifier;
        private readonly ILogger<OrderGrpcService> _logger;

        public OrderGrpcService(
            ISender mediator,
            IOrderRepository orderRepository,
            IOrderStatusNotifier statusNotifier,
            ILogger<OrderGrpcService> logger)
        {
            _mediator = mediator;
            _orderRepository = orderRepository;
            _statusNotifier = statusNotifier;
            _logger = logger;
        }

        /// <summary>
        /// Recibe una orden, la valida y la envia al pipeline de MediatR para su procesamiento.
        /// </summary>
        public override async Task<SubmitOrderReply> SubmitOrder(SubmitOrderRequest request, ServerCallContext context)
        {
            ValidateRequest(request);

            var orderId = Guid.TryParse(request.OrderId, out var parsedId) ? parsedId : Guid.NewGuid();

            var command = new SubmitOrderCommand(
                orderId,
                request.UserId,
                request.Items.Select(MapItem).ToList(),
                (decimal)request.TotalAmount,
                request.PaymentToken);

            // No se registra el PaymentToken (dato sensible) en los logs.
            _logger.LogInformation("Orden recibida {OrderId} para el usuario {UserId}", orderId, request.UserId);

            var order = await _mediator.Send(command, context.CancellationToken);

            return new SubmitOrderReply
            {
                OrderId = order.OrderId.ToString(),
                Status = order.Status.ToString()
            };
        }

        /// <summary>
        /// Transmite el estado de la orden: primero emite el estado actual (snapshot) y luego
        /// los cambios en vivo, hasta alcanzar un estado terminal o cerrarse el cliente.
        /// </summary>
        public override async Task TrackOrderStatus(
            TrackOrderRequest request,
            IServerStreamWriter<OrderStatusReply> responseStream,
            ServerCallContext context)
        {
            if (string.IsNullOrWhiteSpace(request.OrderId))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "OrderId es requerido."));
            }

            var snapshot = await _orderRepository.GetByIdAsync(request.OrderId);
            if (snapshot is not null)
            {
                await responseStream.WriteAsync(BuildReply(snapshot.OrderId.ToString(), snapshot.Status, snapshot.UpdatedAt));

                if (IsTerminal(snapshot.Status))
                {
                    return;
                }
            }

            await foreach (var update in _statusNotifier.SubscribeAsync(request.OrderId, context.CancellationToken))
            {
                await responseStream.WriteAsync(new OrderStatusReply
                {
                    OrderId = update.OrderId,
                    Status = update.Status,
                    Timestamp = update.Timestamp.ToString("O")
                });

                if (Enum.TryParse<OrderStatus>(update.Status, out var status) && IsTerminal(status))
                {
                    break;
                }
            }
        }

        private static OrderItemDto MapItem(OrderItemMessage item)
        {
            var productId = Guid.TryParse(item.ProductId, out var id) ? id : Guid.Empty;
            return new OrderItemDto(productId, item.Quantity, (decimal)item.UnitPrice);
        }

        private static OrderStatusReply BuildReply(string orderId, OrderStatus status, DateTime timestamp) => new()
        {
            OrderId = orderId,
            Status = status.ToString(),
            Timestamp = timestamp.ToString("O")
        };

        private static bool IsTerminal(OrderStatus status) =>
            status is OrderStatus.ShipmentRequested or OrderStatus.Rejected;

        private static void ValidateRequest(SubmitOrderRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.UserId))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "UserId es requerido."));
            }

            if (string.IsNullOrWhiteSpace(request.PaymentToken))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "PaymentToken es requerido."));
            }

            if (request.TotalAmount <= 0)
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "TotalAmount debe ser mayor a cero."));
            }

            if (request.Items.Count == 0)
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "La orden debe contener al menos un item."));
            }
        }
    }
}
