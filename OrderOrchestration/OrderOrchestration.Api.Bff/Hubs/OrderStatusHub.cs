using Microsoft.AspNetCore.SignalR;
using OrderOrchestration.Api.Bff.Services;

namespace OrderOrchestration.Api.Bff.Hubs
{
    /// <summary>
    /// Hub de SignalR para el seguimiento de órdenes en tiempo real. Los clientes (Angular)
    /// se suscriben a una orden por su id; el BFF reenvía hacia este hub las actualizaciones
    /// recibidas del stream gRPC <c>TrackOrderStatus</c>.
    /// </summary>
    public class OrderStatusHub : Hub
    {
        private readonly IOrderStatusRelay _statusRelay;

        public OrderStatusHub(IOrderStatusRelay statusRelay)
        {
            _statusRelay = statusRelay;
        }

        /// <summary>
        /// Suscribe la conexión actual a las actualizaciones de una orden y garantiza que
        /// el reenvío del stream gRPC esté activo para esa orden.
        /// </summary>
        public async Task Subscribe(string orderId)
        {
            if (string.IsNullOrWhiteSpace(orderId))
            {
                throw new HubException("OrderId es requerido.");
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, orderId);
            _statusRelay.EnsureRelay(orderId);
        }

        /// <summary>
        /// Cancela la suscripción de la conexión actual a una orden.
        /// </summary>
        public Task Unsubscribe(string orderId) =>
            Groups.RemoveFromGroupAsync(Context.ConnectionId, orderId);
    }
}
