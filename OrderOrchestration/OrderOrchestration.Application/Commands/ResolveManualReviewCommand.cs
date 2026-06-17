using MediatR;
using OrderOrchestration.Domain;

namespace OrderOrchestration.Application.Commands
{
    /// <summary>
    /// Comando para resolver manualmente (human-in-the-loop) una orden que está en
    /// estado <c>ManualReviewRequired</c>. Devuelve la orden actualizada, o <c>null</c>
    /// si no existe o no está en revisión manual.
    /// </summary>
    public record ResolveManualReviewCommand(Guid OrderId, bool Approved, string Reviewer) : IRequest<Order?>;
}
