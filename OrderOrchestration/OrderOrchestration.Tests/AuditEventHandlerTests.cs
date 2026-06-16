using Moq;
using OrderOrchestration.Application.Auditing;
using OrderOrchestration.Domain;
using OrderOrchestration.Domain.Data;
using OrderOrchestration.Domain.Events;
using Xunit;

namespace OrderOrchestration.Tests
{
    public class AuditEventHandlerTests
    {
        private readonly Mock<IAuditRepository> _auditRepository = new();

        private AuditEventHandler CreateSut() => new(_auditRepository.Object);

        [Fact]
        public async Task Handle_OrderSubmitted_WritesAuditLogWithExpectedAction()
        {
            // Arrange
            AuditLog? captured = null;
            _auditRepository.Setup(r => r.AddLogAsync(It.IsAny<AuditLog>()))
                .Callback<AuditLog>(log => captured = log)
                .Returns(Task.CompletedTask);
            var sut = CreateSut();
            var orderId = Guid.NewGuid().ToString();

            // Act
            await sut.Handle(new OrderSubmittedEvent(orderId, "user-1", 100m, new List<OrderItem>(), "tok_123456789"), CancellationToken.None);

            // Assert
            Assert.NotNull(captured);
            Assert.Equal("OrderSubmitted", captured!.Action);
            Assert.Equal(orderId, captured.OrderId);
        }

        [Fact]
        public async Task Handle_PaymentProcessed_MasksPaymentTokenInAuditDetails()
        {
            // Arrange
            AuditLog? captured = null;
            _auditRepository.Setup(r => r.AddLogAsync(It.IsAny<AuditLog>()))
                .Callback<AuditLog>(log => captured = log)
                .Returns(Task.CompletedTask);
            var sut = CreateSut();

            // Act
            await sut.Handle(new PaymentProcessedEvent(Guid.NewGuid().ToString(), "tok_123456789"), CancellationToken.None);

            // Assert - el token nunca debe quedar en claro en la auditoría
            Assert.NotNull(captured);
            Assert.DoesNotContain("tok_123456789", captured!.Details);
            Assert.Contains("****6789", captured.Details);
        }
    }
}
