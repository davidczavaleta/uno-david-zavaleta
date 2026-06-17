namespace OrderOrchestration.Domain.Data
{
    public interface IOrderRepository
    {
        /// <summary>
        /// Obtiene una orden por su id incluyendo datos y eventos de dominio.
        /// </summary>
        /// <param name="orderId"></param>
        /// <returns></returns>
        Task<Order?> GetByIdAsync(string orderId);

        /// <summary>
        /// Obtiene duplicados por token de pago para evitar procesar el mismo pago más de una vez (idempotencia)
        /// </summary>
        /// <param name="paymentToken"></param>
        /// <returns></returns>
        Task<Order?> GetByPaymentTokenAsync(string paymentToken);

        /// <summary>
        /// Guarda la orden junto con sus eventos de dominio en una transacción atómica, para luego ser procesados por el worker del outbox pattern
        /// </summary>
        /// <param name="order"></param>
        /// <returns></returns>
        Task SaveAsync(Order order);

        /// <summary>
        /// Obtiene todas las órdenes que tienen eventos de dominio en su bandeja de salida (outbox) que aún no han sido procesados, para que el worker los envíe al bus de mensajes. 
        /// </summary>
        /// <returns></returns>
        Task<List<Order>> GetOrdersWithUnprocessedEventsAsync();

        /// <summary>
        /// Marca un mensaje específico del outbox como procesado mediante una actualización
        /// atómica posicional, sin sobrescribir el resto del documento (evita pérdida de cambios
        /// de estado realizados concurrentemente por el orquestador).
        /// </summary>
        /// <param name="orderId">Id de la orden propietaria del mensaje.</param>
        /// <param name="messageId">Id del mensaje del outbox a marcar como procesado.</param>
        Task MarkOutboxMessageProcessedAsync(Guid orderId, Guid messageId);

        /// <summary>
        /// Obtiene todas las órdenes que se encuentran en el estado indicado.
        /// Usado, por ejemplo, para listar las órdenes pendientes de revisión manual.
        /// </summary>
        Task<List<Order>> GetByStatusAsync(OrderStatus status);
    }
}
