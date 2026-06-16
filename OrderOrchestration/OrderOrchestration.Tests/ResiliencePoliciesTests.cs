using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using OrderOrchestration.Infrastructure.Resilience;
using Polly;
using Polly.Registry;
using Polly.Timeout;
using Xunit;

namespace OrderOrchestration.Tests
{
    public class ResiliencePoliciesTests
    {
        private static ResiliencePipeline GetPipeline()
        {
            var services = new ServiceCollection();
            services.AddGrpcResiliencePipeline();
            var provider = services.BuildServiceProvider();
            var pipelineProvider = provider.GetRequiredService<ResiliencePipelineProvider<string>>();
            return pipelineProvider.GetPipeline(ResiliencePolicies.GrpcFraudPipeline);
        }

        [Fact]
        public async Task Pipeline_WhenTransientFailuresThenSuccess_RetriesUntilItSucceeds()
        {
            // Arrange
            var pipeline = GetPipeline();
            var attempts = 0;

            // Act
            var result = await pipeline.ExecuteAsync(async _ =>
            {
                attempts++;
                if (attempts < 3)
                {
                    throw new RpcException(new Status(StatusCode.Unavailable, "transient"));
                }

                await Task.CompletedTask;
                return "ok";
            }, CancellationToken.None);

            // Assert
            Assert.Equal("ok", result);
            Assert.Equal(3, attempts);
        }

        [Fact]
        public async Task Pipeline_WhenCallExceedsTimeout_RaisesTimeoutAndRetries()
        {
            // Arrange
            var pipeline = GetPipeline();
            var attempts = 0;

            // Act + Assert: una respuesta más lenta que el timeout (1s) provoca TimeoutRejectedException,
            // y la estrategia de retry debe reintentar (más de un intento).
            await Assert.ThrowsAsync<TimeoutRejectedException>(async () =>
            {
                await pipeline.ExecuteAsync(async ct =>
                {
                    attempts++;
                    await Task.Delay(TimeSpan.FromSeconds(5), ct);
                }, CancellationToken.None);
            });

            Assert.True(attempts > 1, $"Se esperaban múltiples intentos, pero hubo {attempts}.");
        }
    }
}
