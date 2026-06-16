using OrderOrchestration.Application.Contracts;
using OrderOrchestration.Infrastructure.Streaming;
using Xunit;

namespace OrderOrchestration.Tests
{
    public class InMemoryOrderStatusNotifierTests
    {
        [Fact]
        public async Task SubscribeAsync_ReceivesPublishedUpdatesForTheSameOrder()
        {
            // Arrange
            var notifier = new InMemoryOrderStatusNotifier();
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            const string orderId = "order-1";
            var received = new List<OrderStatusUpdate>();

            var consumer = Task.Run(async () =>
            {
                await foreach (var update in notifier.SubscribeAsync(orderId, cts.Token))
                {
                    received.Add(update);
                    if (received.Count == 1)
                    {
                        break;
                    }
                }
            });

            // Da tiempo a que la suscripción se registre antes de publicar.
            await Task.Delay(150);

            // Act
            await notifier.PublishAsync(new OrderStatusUpdate(orderId, "Approved", DateTime.UtcNow));
            await consumer;

            // Assert
            var update = Assert.Single(received);
            Assert.Equal(orderId, update.OrderId);
            Assert.Equal("Approved", update.Status);
        }

        [Fact]
        public async Task PublishAsync_WithNoSubscribers_DoesNotThrow()
        {
            // Arrange
            var notifier = new InMemoryOrderStatusNotifier();

            // Act + Assert
            await notifier.PublishAsync(new OrderStatusUpdate("order-unknown", "Pending", DateTime.UtcNow));
        }
    }
}
