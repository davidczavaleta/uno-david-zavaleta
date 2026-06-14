using MediatR;
using OrderOrchestration.Domain;

namespace OrderOrchestration.Application.Commands
{    
    public record SubmitOrderCommand(Guid OrderId, string UserId, List<OrderItemDto> Items, decimal TotalAmount, string PaymentToken) : IRequest<Order>;

    /// <summary>
    /// Dto interno para representar los items de la orden.
    /// </summary>    
    public record OrderItemDto
    (
        Guid ProductId,
        int Quantity,
        decimal UnitPrice
    );

}
