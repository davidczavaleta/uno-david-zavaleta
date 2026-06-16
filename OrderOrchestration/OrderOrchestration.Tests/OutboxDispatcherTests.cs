using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using OrderOrchestration.Domain;
using OrderOrchestration.Domain.Data;
using OrderOrchestration.Domain.Events;
using OrderOrchestration.Infrastructure.Outbox;
using Xunit;

namespace OrderOrchestration.Tests
{
    public class OutboxDispatcherTests
    {
        [Fact]
        public async Task Dispatcher_WithUnprocessedMessage_PublishesEventAndMarksItProcessed()
        {
            // Arrange
            var order = TestData.CreateOrder(OrderStatus.Pending);
            order.AddDomainEvent(new OrderSubmittedEvent(
                order.OrderId.ToString(), order.UserId, order.TotalAmount, order.Items, order.PaymentToken));
            var message = order.OutboxMessages.Single();

            var orderRepository = new Mock<IOrderRepository>();
            orderRepository.SetupSequence(r => r.GetOrdersWithUnprocessedEventsAsync())
                .ReturnsAsync(new List<Order> { order })
                .ReturnsAsync(new List<Order>());
            orderRepository.Setup(r => r.MarkOutboxMessageProcessedAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
                .Returns(Task.CompletedTask);

            var publisher = new Mock<IPublisher>();
            publisher.Setup(p => p.Publish(It.IsAny<DomainEvent>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var scopeFactory = BuildScopeFactory(orderRepository.Object, publisher.Object);
            var dispatcher = new OutboxDispatcher(scopeFactory, Mock.Of<ILogger<OutboxDispatcher>>());

            // Act - se ejecuta brevemente y se detiene
            await dispatcher.StartAsync(CancellationToken.None);
            await WaitUntilAsync(() =>
                publisher.Invocations.Any(i => i.Method.Name == nameof(IPublisher.Publish)));
            await dispatcher.StopAsync(CancellationToken.None);

            // Assert
            publisher.Verify(p => p.Publish(It.Is<DomainEvent>(e => e is OrderSubmittedEvent), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
            orderRepository.Verify(r => r.MarkOutboxMessageProcessedAsync(order.OrderId, message.Id), Times.AtLeastOnce);
        }

        private static IServiceScopeFactory BuildScopeFactory(IOrderRepository orderRepository, IPublisher publisher)
        {
            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider.Setup(p => p.GetService(typeof(IOrderRepository))).Returns(orderRepository);
            serviceProvider.Setup(p => p.GetService(typeof(IPublisher))).Returns(publisher);

            var scope = new Mock<IServiceScope>();
            scope.Setup(s => s.ServiceProvider).Returns(serviceProvider.Object);

            var scopeFactory = new Mock<IServiceScopeFactory>();
            scopeFactory.Setup(f => f.CreateScope()).Returns(scope.Object);

            return scopeFactory.Object;
        }

        private static async Task WaitUntilAsync(Func<bool> condition, int timeoutMs = 3000)
        {
            var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
            while (DateTime.UtcNow < deadline)
            {
                if (condition())
                {
                    return;
                }

                await Task.Delay(50);
            }
        }
    }
}
