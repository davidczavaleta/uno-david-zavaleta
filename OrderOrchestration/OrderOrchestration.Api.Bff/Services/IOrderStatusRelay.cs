namespace OrderOrchestration.Api.Bff.Services
{
    /// <summary>
    /// Reenvía las actualizaciones de estado de una orden desde el stream gRPC
    /// <c>TrackOrderStatus</c> hacia los clientes conectados por SignalR.
    /// </summary>
    public interface IOrderStatusRelay
    {
        /// <summary>
        /// Garantiza que exista un único reenvío activo para la orden indicada.
        /// Es idempotente: llamadas repetidas para la misma orden no crean streams duplicados.
        /// </summary>
        void EnsureRelay(string orderId);
    }
}
