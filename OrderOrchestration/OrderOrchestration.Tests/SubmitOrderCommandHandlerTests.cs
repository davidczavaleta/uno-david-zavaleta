using Moq;
using OrderOrchestration.Application.Commands;
using OrderOrchestration.Domain;
using OrderOrchestration.Domain.Data;
using OrderOrchestration.Domain.Events;
using Xunit;

namespace OrderOrchestration.Tests
{
    public class SubmitOrderCommandHandlerTests
    {
        private readonly Mock<IOrderRepository> _orderRepository = new();

        private SubmitOrderCommandHandler CreateSut() => new(_orderRepository.Object);

        [Fact]
        public async Task Handle_WithNewPaymentToken_PersistsPendingOrderAndEmitsOrderSubmittedEvent()
        {
            // Arrange
            _orderRepository.Setup(r => r.GetByPaymentTokenAsync(It.IsAny<string>())).ReturnsAsync((Order?)null);
            var command = BuildCommand();
            var sut = CreateSut();

            // Act
            var result = await sut.Handle(command, CancellationToken.None);

            // Assert
            Assert.Equal(OrderStatus.Pending, result.Status);
            Assert.Contains(result.OutboxMessages, m => m.Type.Contains(nameof(OrderSubmittedEvent)));
            _orderRepository.Verify(r => r.SaveAsync(It.IsAny<Order>()), Times.Once);
        }

        [Fact]
        public async Task Handle_WithExistingPaymentToken_IsIdempotentAndDoesNotSaveAgain()
        {
            // Arrange - simula un reintento del cliente con el mismo token de idempotencia
            var existing = TestData.CreateOrder(OrderStatus.Approved, paymentToken: "tok_existing");
            _orderRepository.Setup(r => r.GetByPaymentTokenAsync("tok_existing")).ReturnsAsync(existing);
            var command = BuildCommand(paymentToken: "tok_existing");
            var sut = CreateSut();

            // Act
            var result = await sut.Handle(command, CancellationToken.None);

            // Assert
            Assert.Same(existing, result);
            _orderRepository.Verify(r => r.SaveAsync(It.IsAny<Order>()), Times.Never);
        }

        private static SubmitOrderCommand BuildCommand(string paymentToken = "tok_new") =>
            new(
                Guid.NewGuid(),
                "user-1",
                new List<OrderItemDto> { new(Guid.NewGuid(), 1, 100m) },
                100m,
                paymentToken);
    }
}
