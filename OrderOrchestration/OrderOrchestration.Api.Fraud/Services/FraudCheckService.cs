using Grpc.Core;

namespace OrderOrchestration.Api.Fraud.Services
{
    public class FraudCheckService : FraudService.FraudServiceBase
    {
        private readonly ILogger<FraudCheckService> _logger;
        private static readonly Random _random = new();

        public FraudCheckService(ILogger<FraudCheckService> logger)
        {
            _logger = logger;
        }

        public override async Task<FraudCheckResponse> CheckFraud(FraudCheckRequest request, ServerCallContext context)
        {
            // 30% de probabilidad de simular latencia de 2 segundos
            // Dummy para activar el Timeout de Polly
            var isSlowResponse = _random.Next(100) < 30;
            if (isSlowResponse)
            {
                _logger.LogWarning("Sobrecarga del servicio: respuesta lenta de 2s para la orden {OrderId}", request.OrderId);
                await Task.Delay(TimeSpan.FromSeconds(2), context.CancellationToken);
            }

            // 50% de probabilidad de aprobar o rechazar (aleatorio)
            var isApproved = _random.Next(2) == 0;
            var reason = isApproved ? "Verificación exitosa" : "La orden disparo todas nuestras alertas de fraude";

            _logger.LogInformation("Verificación de fraude: {IsApproved} - {Reason} para la orden {OrderId}", isApproved, reason, request.OrderId);

            return new FraudCheckResponse
            {
                IsApproved = isApproved,
                Reason = reason
            };
        }
    }
}