using Grpc.Core;
using Microsoft.AspNetCore.Mvc;
using OrderOrchestration.Api.Bff.Models;
using OrderOrchestration.Api.Bff.Services;
using OrderOrchestration.Api.Core;

namespace OrderOrchestration.Api.Bff.Controllers
{
    /// <summary>
    /// Fachada REST de mediacion de protocolo para los clientes. 
    /// Traduce las peticiones HTTP a llamadas gRPC contra el servicio de órdenes (Api.Core).     
    /// </summary>
    [ApiController]
    [Route("api/orders")]
    public class OrdersController : ControllerBase
    {
        private readonly OrderService.OrderServiceClient _orderClient;
        private readonly IOrderStatusRelay _statusRelay;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(
            OrderService.OrderServiceClient orderClient,
            IOrderStatusRelay statusRelay,
            ILogger<OrdersController> logger)
        {
            _orderClient = orderClient;
            _statusRelay = statusRelay;
            _logger = logger;
        }

        /// <summary>
        /// Envía una nueva orden. Devuelve el identificador y el estado inicial; el seguimiento
        /// en tiempo real se realiza vía SignalR (hub /hubs/orders).
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<SubmitOrderResponseDto>> SubmitOrder(
            [FromBody] SubmitOrderRequestDto request,
            CancellationToken cancellationToken)
        {
            var grpcRequest = new SubmitOrderRequest
            {
                OrderId = request.OrderId ?? string.Empty,
                UserId = request.UserId,
                TotalAmount = (double)request.TotalAmount,
                PaymentToken = request.PaymentToken
            };

            grpcRequest.Items.AddRange(request.Items.Select(item => new OrderItemMessage
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = (double)item.UnitPrice
            }));

            try
            {
                var reply = await _orderClient.SubmitOrderAsync(grpcRequest, cancellationToken: cancellationToken);

                // El reenvío se iniciará únicamente cuando el cliente Angular llame a Subscribe en el Hub,
                // para evitar una condición de carrera (SignalR emite el snapshot antes de que el cliente se haya unido al grupo).
                
                return Ok(new SubmitOrderResponseDto(reply.OrderId, reply.Status));
            }
            catch (RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.InvalidArgument)
            {
                return BadRequest(new { error = ex.Status.Detail });
            }
            catch (RpcException ex)
            {
                _logger.LogError(ex, "Fallo al enviar la orden al servicio gRPC.");
                return StatusCode(502, new { error = "No se pudo procesar la orden. Inténtelo de nuevo." });
            }
        }

        /// <summary>
        /// Lista las órdenes pendientes de revisión manual (panel admin).
        /// </summary>
        [HttpGet("pending-reviews")]
        public async Task<ActionResult<IEnumerable<PendingReviewDto>>> GetPendingReviews(CancellationToken cancellationToken)
        {
            try
            {
                var reply = await _orderClient.GetPendingReviewsAsync(new GetPendingReviewsRequest(), cancellationToken: cancellationToken);

                var result = reply.Reviews.Select(r => new PendingReviewDto(
                    r.OrderId, r.UserId, (decimal)r.TotalAmount, r.CreatedAt));

                return Ok(result);
            }
            catch (RpcException ex)
            {
                _logger.LogError(ex, "Fallo al obtener las órdenes pendientes de revisión.");
                return StatusCode(502, new { error = "No se pudo obtener la lista. Inténtelo de nuevo." });
            }
        }

        /// <summary>
        /// Resuelve manualmente (human-in-the-loop) una orden en revisión.
        /// </summary>
        [HttpPost("{id}/review")]
        public async Task<ActionResult<SubmitOrderResponseDto>> ResolveReview(
            string id,
            [FromBody] ReviewRequestDto request,
            CancellationToken cancellationToken)
        {
            try
            {
                var reply = await _orderClient.ResolveManualReviewAsync(new ResolveManualReviewRequest
                {
                    OrderId = id,
                    Approved = request.Approved,
                    Reviewer = request.Reviewer
                }, cancellationToken: cancellationToken);

                // Reanuda el reenvío del stream para reflejar el avance tras la resolución.
                _statusRelay.EnsureRelay(reply.OrderId);

                return Ok(new SubmitOrderResponseDto(reply.OrderId, reply.Status));
            }
            catch (RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.InvalidArgument)
            {
                return BadRequest(new { error = ex.Status.Detail });
            }
            catch (RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.FailedPrecondition)
            {
                return Conflict(new { error = ex.Status.Detail });
            }
            catch (RpcException ex)
            {
                _logger.LogError(ex, "Fallo al resolver la revisión manual.");
                return StatusCode(502, new { error = "No se pudo resolver la revisión. Inténtelo de nuevo." });
            }
        }
    }
}
