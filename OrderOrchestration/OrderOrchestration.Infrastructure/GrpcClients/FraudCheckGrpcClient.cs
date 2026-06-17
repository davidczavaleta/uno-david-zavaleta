using OrderOrchestration.Application.Contracts;
using OrderOrchestration.Domain;
using Polly;
using Polly.Registry;
using OrderOrchestration.Infrastructure.Resilience;

namespace OrderOrchestration.Infrastructure.GrpcClients;

public class FraudCheckGrpcClient : IFraudCheckService
{
    private readonly FraudService.FraudServiceClient _grpcClient;
    private readonly ResiliencePipeline _pipeline;

    public FraudCheckGrpcClient(FraudService.FraudServiceClient grpcClient, ResiliencePipelineProvider<string> pipelineProvider)
    {
        _grpcClient = grpcClient;
        _pipeline = pipelineProvider.GetPipeline(ResiliencePolicies.GrpcFraudPipeline);
    }

    /// <inheritdoc />
    public async Task<FraudCheckResult> CheckFraudAsync(string orderId, string userId, decimal totalAmount, CancellationToken cancellationToken = default)
    {
        var response = await _pipeline.ExecuteAsync(async ct =>
        {
            return await _grpcClient.CheckFraudAsync(new FraudCheckRequest
            {
                OrderId = orderId,
                UserId = userId,
                TotalAmount = (double)totalAmount
            }, cancellationToken: ct);
        }, cancellationToken);

        return new FraudCheckResult(MapDecision(response), response.Reason);
    }

    /// <summary>
    /// Traduce la decisión del contrato gRPC (enum del proto) al enum de dominio. Si el servicio
    /// no especifica decisión (valor por defecto), se usa el campo legado <c>is_approved</c> como respaldo.
    /// </summary>
    private static OrderOrchestration.Domain.FraudDecision MapDecision(FraudCheckResponse response) => response.Decision switch
    {
        OrderOrchestration.Infrastructure.FraudDecision.Approved => OrderOrchestration.Domain.FraudDecision.Approved,
        OrderOrchestration.Infrastructure.FraudDecision.Rejected => OrderOrchestration.Domain.FraudDecision.Rejected,
        OrderOrchestration.Infrastructure.FraudDecision.ManualReview => OrderOrchestration.Domain.FraudDecision.ManualReview,
        _ => response.IsApproved ? OrderOrchestration.Domain.FraudDecision.Approved : OrderOrchestration.Domain.FraudDecision.Rejected
    };
}
