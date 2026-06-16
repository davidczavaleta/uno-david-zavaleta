using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;

namespace OrderOrchestration.Infrastructure.Resilience;

public static class ResiliencePolicies
{
    public const string GrpcFraudPipeline = "grpc-fraud-pipeline";

    /// <summary>
    /// Registra un pipeline de resiliencia reutilizable con Retry + Circuit Breaker.
    /// </summary>
    public static IServiceCollection AddGrpcResiliencePipeline(this IServiceCollection services)
    {
        services.AddResiliencePipeline(GrpcFraudPipeline, builder =>
        {
            // 1. Retry (estrategia más externa): 3 reintentos con espera exponencial.
            //    Maneja tanto fallos gRPC como los timeouts por intento (TimeoutRejectedException),
            //    de modo que una respuesta lenta del servicio de fraude provoque un reintento.
            builder.AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromMilliseconds(200),
                BackoffType = DelayBackoffType.Exponential,
                ShouldHandle = new PredicateBuilder()
                    .Handle<Grpc.Core.RpcException>()
                    .Handle<TimeoutRejectedException>()
            });

            // 2. Circuit Breaker: se abre si falla el 50% de las últimas 10 peticiones
            builder.AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                FailureRatio = 0.5,
                SamplingDuration = TimeSpan.FromSeconds(30),
                MinimumThroughput = 10,
                BreakDuration = TimeSpan.FromSeconds(15),
                ShouldHandle = new PredicateBuilder()
                    .Handle<Grpc.Core.RpcException>()
                    .Handle<TimeoutRejectedException>()
            });

            // 3. Timeout (estrategia más interna): máximo 1 segundo por intento.
            builder.AddTimeout(TimeSpan.FromSeconds(1));
        });

        return services;
    }
}
