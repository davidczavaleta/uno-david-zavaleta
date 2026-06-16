namespace OrderOrchestration.Application.Contracts;

public interface IFraudCheckService
{
    /// <summary>
    /// Verifica si una orden es sospechosa de fraude.
    /// </summary>
    Task<FraudCheckResult> CheckFraudAsync(string orderId, string userId, decimal totalAmount, CancellationToken cancellationToken = default);
}

public record FraudCheckResult(bool IsApproved, string Reason);