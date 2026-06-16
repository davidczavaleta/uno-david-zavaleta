using OrderOrchestration.Application.Contracts;
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

        return new FraudCheckResult(response.IsApproved, response.Reason);
    }
}
