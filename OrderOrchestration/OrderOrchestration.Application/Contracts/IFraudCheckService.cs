using OrderOrchestration.Domain;

namespace OrderOrchestration.Application.Contracts;

public interface IFraudCheckService
{
    /// <summary>
    /// Verifica si una orden es sospechosa de fraude.
    /// </summary>
    Task<FraudCheckResult> CheckFraudAsync(string orderId, string userId, decimal totalAmount, CancellationToken cancellationToken = default);
}

/// <summary>
/// Resultado de la verificación de fraude: la decisión (aprobada, rechazada o revisión manual)
/// y el motivo asociado.
/// </summary>
public record FraudCheckResult(FraudDecision Decision, string Reason);
