using System.Collections.Concurrent;
using Grpc.Core;
using Microsoft.AspNetCore.SignalR;
using OrderOrchestration.Api.Bff.Hubs;
using OrderOrchestration.Api.Bff.Models;
using OrderOrchestration.Api.Core;

namespace OrderOrchestration.Api.Bff.Services
{
    /// <summary>
    /// Implementación singleton que consume el stream gRPC <c>TrackOrderStatus</c> de Api.Core
    /// y reenvía cada actualización al grupo SignalR correspondiente a la orden.
    /// Mantiene un único reenvío por orden (deduplicado) que finaliza al alcanzar un estado terminal.
    /// </summary>
    public class OrderStatusRelay : IOrderStatusRelay
    {
        private static readonly HashSet<string> TerminalStatuses = new(StringComparer.OrdinalIgnoreCase)
        {
            "ShipmentRequested",
            "Rejected"
        };

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHubContext<OrderStatusHub> _hubContext;
        private readonly ILogger<OrderStatusRelay> _logger;
        private readonly ConcurrentDictionary<string, Task> _activeRelays = new();

        public OrderStatusRelay(
            IServiceScopeFactory scopeFactory,
            IHubContext<OrderStatusHub> hubContext,
            ILogger<OrderStatusRelay> logger)
        {
            _scopeFactory = scopeFactory;
            _hubContext = hubContext;
            _logger = logger;
        }

        /// <inheritdoc />
        public void EnsureRelay(string orderId)
        {
            _activeRelays.GetOrAdd(orderId, id => Task.Run(() => RelayAsync(id)));
        }

        private async Task RelayAsync(string orderId)
        {
            // El cliente gRPC tipado es transient; se resuelve dentro de un scope que vive
            // mientras dure el stream para evitar dependencias cautivas en este singleton.
            using var scope = _scopeFactory.CreateScope();
            var client = scope.ServiceProvider.GetRequiredService<OrderService.OrderServiceClient>();

            try
            {
                using var call = client.TrackOrderStatus(new TrackOrderRequest { OrderId = orderId });

                await foreach (var update in call.ResponseStream.ReadAllAsync())
                {
                    await _hubContext.Clients
                        .Group(orderId)
                        .SendAsync("OrderStatusChanged", new OrderStatusDto(update.OrderId, update.Status, update.Timestamp));

                    if (TerminalStatuses.Contains(update.Status))
                    {
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fallo el reenvío de estado para la orden {OrderId}.", orderId);
            }
            finally
            {
                _activeRelays.TryRemove(orderId, out _);
            }
        }
    }
}
