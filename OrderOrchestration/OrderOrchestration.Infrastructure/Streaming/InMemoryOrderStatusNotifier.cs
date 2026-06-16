using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using OrderOrchestration.Application.Contracts;

namespace OrderOrchestration.Infrastructure.Streaming;

/// <summary>
/// Implementacion in-process de <see cref="IOrderStatusNotifier"/> basada en
/// <see cref="System.Threading.Channels"/>. Cada suscriptor recibe su propio canal,
/// por lo que multiples clientes pueden seguir la misma orden simultaneamente.
/// Debe registrarse como singleton para compartir el estado entre productores y consumidores.
/// </summary>
public class InMemoryOrderStatusNotifier : IOrderStatusNotifier
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<Guid, Channel<OrderStatusUpdate>>> _subscribers = new();

    /// <inheritdoc />
    public async ValueTask PublishAsync(OrderStatusUpdate update, CancellationToken cancellationToken = default)
    {
        if (!_subscribers.TryGetValue(update.OrderId, out var orderSubscribers))
        {
            return;
        }

        foreach (var channel in orderSubscribers.Values)
        {
            await channel.Writer.WriteAsync(update, cancellationToken);
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<OrderStatusUpdate> SubscribeAsync(
        string orderId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var subscriptionId = Guid.NewGuid();
        var channel = Channel.CreateUnbounded<OrderStatusUpdate>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

        var orderSubscribers = _subscribers.GetOrAdd(orderId, _ => new ConcurrentDictionary<Guid, Channel<OrderStatusUpdate>>());
        orderSubscribers[subscriptionId] = channel;

        try
        {
            await foreach (var update in channel.Reader.ReadAllAsync(cancellationToken))
            {
                yield return update;
            }
        }
        finally
        {
            orderSubscribers.TryRemove(subscriptionId, out _);
            if (orderSubscribers.IsEmpty)
            {
                _subscribers.TryRemove(orderId, out _);
            }
        }
    }
}
