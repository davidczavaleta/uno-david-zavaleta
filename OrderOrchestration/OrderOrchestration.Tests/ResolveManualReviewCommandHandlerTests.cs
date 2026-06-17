using Moq;
using OrderOrchestration.Application.Commands;
using OrderOrchestration.Domain;
using OrderOrchestration.Domain.Data;
using OrderOrchestration.Domain.Events;
using Xunit;

namespace OrderOrchestration.Tests
{
    public class ResolveManualReviewCommandHandlerTests
    {
        private readonly Mock<IOrderRepository> _orderRepository = new();

        private ResolveManualReviewCommandHandler CreateSut() => new(_orderRepository.Object);

        [Fact]
        public async Task Handle_WhenOrderInManualReview_EmitsResolvedEventAndSaves()
        {
            // Arrange
            var order = TestData.CreateOrder(OrderStatus.ManualReviewRequired);
            _orderRepository.Setup(r => r.GetByIdAsync(order.OrderId.ToString())).ReturnsAsync(order);
            var sut = CreateSut();

            // Act
            var result = await sut.Handle(new ResolveManualReviewCommand(order.OrderId, true, "operador-1"), CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Contains(order.OutboxMessages, m => m.Type.Contains(nameof(ManualReviewResolvedEvent)));
            _orderRepository.Verify(r => r.SaveAsync(order), Times.Once);
        }

        [Fact]
        public async Task Handle_WhenOrderNotInManualReview_ReturnsNullAndDoesNotSave()
        {
            // Arrange - la orden no está en revisión manual
            var order = TestData.CreateOrder(OrderStatus.Approved);
            _orderRepository.Setup(r => r.GetByIdAsync(order.OrderId.ToString())).ReturnsAsync(order);
            var sut = CreateSut();

            // Act
            var result = await sut.Handle(new ResolveManualReviewCommand(order.OrderId, true, "operador-1"), CancellationToken.None);

            // Assert
            Assert.Null(result);
            _orderRepository.Verify(r => r.SaveAsync(It.IsAny<Order>()), Times.Never);
        }

        [Fact]
        public async Task Handle_WhenOrderDoesNotExist_ReturnsNull()
        {
            // Arrange
            _orderRepository.Setup(r => r.GetByIdAsync(It.IsAny<string>())).ReturnsAsync((Order?)null);
            var sut = CreateSut();

            // Act
            var result = await sut.Handle(new ResolveManualReviewCommand(Guid.NewGuid(), false, "operador-1"), CancellationToken.None);

            // Assert
            Assert.Null(result);
            _orderRepository.Verify(r => r.SaveAsync(It.IsAny<Order>()), Times.Never);
        }
    }
}
