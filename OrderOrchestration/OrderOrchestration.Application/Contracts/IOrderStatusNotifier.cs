namespace OrderOrchestration.Application.Contracts;

/// <summary>
/// Representa una actualizacion de estado de una orden que se transmite a los clientes suscritos al stream de seguimiento.
/// </summary>
public record OrderStatusUpdate(string OrderId, string Status, DateTime Timestamp);

/// <summary>
/// Abstraccion de un canal de notificaciones en tiempo real para los cambios de estado de las ordenes.
/// </summary>
public interface IOrderStatusNotifier
{
    /// <summary>
    /// Publica un cambio de estado para que lo reciban los suscriptores de la orden.
    /// </summary>
    ValueTask PublishAsync(OrderStatusUpdate update, CancellationToken cancellationToken = default);

    /// <summary>
    /// Se suscribe a los cambios de estado de una orden especifica. El flujo continua
    /// hasta que el token de cancelacion se dispara (por ejemplo, al cerrar el stream gRPC).
    /// </summary>
    IAsyncEnumerable<OrderStatusUpdate> SubscribeAsync(string orderId, CancellationToken cancellationToken = default);
}
