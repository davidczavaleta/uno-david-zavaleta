using Moq;
using OrderOrchestration.Application.Contracts;
using OrderOrchestration.Application.Orchestration;
using OrderOrchestration.Domain;
using OrderOrchestration.Domain.Data;
using OrderOrchestration.Domain.Events;
using Xunit;

namespace OrderOrchestration.Tests
{
    public class OrchestratorTests
    {
        private readonly Mock<IOrderRepository> _orderRepository = new();
        private readonly Mock<IFraudCheckService> _fraudCheckService = new();
        private readonly Mock<IOrderStatusNotifier> _statusNotifier = new();

        private Orchestrator CreateSut() =>
            new(_orderRepository.Object, _fraudCheckService.Object, _statusNotifier.Object);

        [Fact]
        public async Task Handle_OrderSubmittedWithPendingOrder_MovesToFraudCheckPendingAndEmitsFraudCheckedEvent()
        {
            // Arrange
            var order = TestData.CreateOrder(OrderStatus.Pending);
            _orderRepository.Setup(r => r.GetByIdAsync(order.OrderId.ToString())).ReturnsAsync(order);
            _fraudCheckService
                .Setup(f => f.CheckFraudAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FraudCheckResult(true, "ok"));
            var sut = CreateSut();

            // Act
            await sut.Handle(BuildSubmittedEvent(order), CancellationToken.None);

            // Assert
            Assert.Equal(OrderStatus.FraudCheckPending, order.Status);
            Assert.Contains(order.OutboxMessages, m => m.Type.Contains(nameof(FraudCheckedEvent)));
            _orderRepository.Verify(r => r.SaveAsync(order), Times.Once);
            _statusNotifier.Verify(n => n.PublishAsync(It.IsAny<OrderStatusUpdate>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_OrderSubmittedWithNonPendingOrder_DoesNothing()
        {
            // Arrange
            var order = TestData.CreateOrder(OrderStatus.Approved);
            _orderRepository.Setup(r => r.GetByIdAsync(order.OrderId.ToString())).ReturnsAsync(order);
            var sut = CreateSut();

            // Act
            await sut.Handle(BuildSubmittedEvent(order), CancellationToken.None);

            // Assert
            _fraudCheckService.Verify(f => f.CheckFraudAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()), Times.Never);
            _orderRepository.Verify(r => r.SaveAsync(It.IsAny<Order>()), Times.Never);
        }

        [Fact]
        public async Task Handle_FraudCheckedApproved_MovesToApprovedAndEmitsOrderApprovedEvent()
        {
            // Arrange
            var order = TestData.CreateOrder(OrderStatus.FraudCheckPending);
            _orderRepository.Setup(r => r.GetByIdAsync(order.OrderId.ToString())).ReturnsAsync(order);
            var sut = CreateSut();

            // Act
            await sut.Handle(new FraudCheckedEvent(order.OrderId.ToString(), true, "ok"), CancellationToken.None);

            // Assert
            Assert.Equal(OrderStatus.Approved, order.Status);
            Assert.Contains(order.OutboxMessages, m => m.Type.Contains(nameof(OrderApprovedEvent)));
            _orderRepository.Verify(r => r.SaveAsync(order), Times.Once);
        }

        [Fact]
        public async Task Handle_FraudCheckedRejected_MovesToRejectedAndEmitsOrderRejectedEvent()
        {
            // Arrange
            var order = TestData.CreateOrder(OrderStatus.FraudCheckPending);
            _orderRepository.Setup(r => r.GetByIdAsync(order.OrderId.ToString())).ReturnsAsync(order);
            var sut = CreateSut();

            // Act
            await sut.Handle(new FraudCheckedEvent(order.OrderId.ToString(), false, "fraude detectado"), CancellationToken.None);

            // Assert
            Assert.Equal(OrderStatus.Rejected, order.Status);
            Assert.Contains(order.OutboxMessages, m => m.Type.Contains(nameof(OrderRejectedEvent)));
        }

        [Fact]
        public async Task Handle_OrderApproved_MovesToPaymentProcessedAndEmitsPaymentProcessedEvent()
        {
            // Arrange
            var order = TestData.CreateOrder(OrderStatus.Approved);
            _orderRepository.Setup(r => r.GetByIdAsync(order.OrderId.ToString())).ReturnsAsync(order);
            var sut = CreateSut();

            // Act
            await sut.Handle(new OrderApprovedEvent(order.OrderId.ToString()), CancellationToken.None);

            // Assert
            Assert.Equal(OrderStatus.PaymentProcessed, order.Status);
            Assert.Contains(order.OutboxMessages, m => m.Type.Contains(nameof(PaymentProcessedEvent)));
        }

        [Fact]
        public async Task Handle_PaymentProcessed_MovesToShipmentRequestedAndEmitsShipmentRequestedEvent()
        {
            // Arrange
            var order = TestData.CreateOrder(OrderStatus.PaymentProcessed);
            _orderRepository.Setup(r => r.GetByIdAsync(order.OrderId.ToString())).ReturnsAsync(order);
            var sut = CreateSut();

            // Act
            await sut.Handle(new PaymentProcessedEvent(order.OrderId.ToString(), order.PaymentToken), CancellationToken.None);

            // Assert
            Assert.Equal(OrderStatus.ShipmentRequested, order.Status);
            Assert.Contains(order.OutboxMessages, m => m.Type.Contains(nameof(ShipmentRequestedEvent)));
        }

        [Fact]
        public async Task Handle_FraudCheckedForMissingOrder_DoesNotSave()
        {
            // Arrange
            _orderRepository.Setup(r => r.GetByIdAsync(It.IsAny<string>())).ReturnsAsync((Order?)null);
            var sut = CreateSut();

            // Act
            await sut.Handle(new FraudCheckedEvent(Guid.NewGuid().ToString(), true, "ok"), CancellationToken.None);

            // Assert
            _orderRepository.Verify(r => r.SaveAsync(It.IsAny<Order>()), Times.Never);
        }

        private static OrderSubmittedEvent BuildSubmittedEvent(Order order) =>
            new(order.OrderId.ToString(), order.UserId, order.TotalAmount, order.Items, order.PaymentToken);
    }
}
