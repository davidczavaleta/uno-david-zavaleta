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

            // Distribución de la decisión: ~20% revisión manual, y del resto 50/50 aprobado/rechazado.
            var roll = _random.Next(100);
            FraudDecision decision;
            string reason;

            if (roll < 20)
            {
                decision = FraudDecision.ManualReview;
                reason = "La orden requiere revisión manual por un operador.";
            }
            else if (roll < 60)
            {
                decision = FraudDecision.Approved;
                reason = "Verificación exitosa.";
            }
            else
            {
                decision = FraudDecision.Rejected;
                reason = "La orden disparó nuestras alertas de fraude.";
            }

            _logger.LogInformation("Verificación de fraude: {Decision} - {Reason} para la orden {OrderId}", decision, reason, request.OrderId);

            return new FraudCheckResponse
            {
                IsApproved = decision == FraudDecision.Approved,
                Reason = reason,
                Decision = decision
            };
        }
    }
}
