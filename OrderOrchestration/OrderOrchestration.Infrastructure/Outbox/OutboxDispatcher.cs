using System.Diagnostics;
using System.Text.Json;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrderOrchestration.Domain.Data;
using OrderOrchestration.Domain.Events;

namespace OrderOrchestration.Infrastructure.Outbox;

/// <summary>
/// Worker del patrón Outbox. Sondea periódicamente las órdenes con eventos de dominio
/// sin procesar, los publica de forma confiable a través del broker in-process (MediatR
/// <see cref="IPublisher"/>) y los marca como procesados de forma atómica.
///
/// Garantiza entrega "al menos una vez": si el proceso falla tras publicar pero antes de
/// marcar el mensaje, el evento se reenvía; los handlers son idempotentes (validan el estado
/// actual de la orden), por lo que un reenvío no produce efectos duplicados.
///
/// Para producción, el <see cref="IPublisher"/> in-process se reemplazaría por un publicador
/// hacia RabbitMQ / Kafka, manteniendo intacta esta lógica de lectura y marcado del outbox.
/// </summary>
public class OutboxDispatcher : BackgroundService
{
    /// <summary>
    /// Fuente de trazas (OpenTelemetry) del worker de outbox. Registrar con
    /// <c>AddSource(OutboxDispatcher.ActivitySourceName)</c>.
    /// </summary>
    public const string ActivitySourceName = "OrderOrchestration.Outbox";
    private static readonly ActivitySource ActivitySource = new(ActivitySourceName);

    private static readonly TimeSpan PollingInterval = TimeSpan.FromMilliseconds(500);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxDispatcher> _logger;

    public OutboxDispatcher(IServiceScopeFactory scopeFactory, ILogger<OutboxDispatcher> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OutboxDispatcher iniciado.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingMessagesAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al procesar la bandeja de salida (outbox).");
            }

            try
            {
                await Task.Delay(PollingInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("OutboxDispatcher detenido.");
    }

    private async Task ProcessPendingMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var orderRepository = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

        var orders = await orderRepository.GetOrdersWithUnprocessedEventsAsync();

        foreach (var order in orders)
        {
            var pendingMessages = order.OutboxMessages
                .Where(m => !m.Processed)
                .OrderBy(m => m.OccurredOn)
                .ToList();

            foreach (var message in pendingMessages)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                var domainEvent = Deserialize(message.Type, message.Content);
                if (domainEvent is null)
                {
                    _logger.LogError("No se pudo deserializar el mensaje de outbox {MessageId} (tipo {Type}). Se marca como procesado para evitar bloqueo.", message.Id, message.Type);
                    await orderRepository.MarkOutboxMessageProcessedAsync(order.OrderId, message.Id);
                    continue;
                }

                using var activity = ActivitySource.StartActivity("Outbox.PublishEvent");
                activity?.SetTag("order.id", order.OrderId.ToString());
                activity?.SetTag("event.type", domainEvent.GetType().Name);

                await publisher.Publish(domainEvent, cancellationToken);
                await orderRepository.MarkOutboxMessageProcessedAsync(order.OrderId, message.Id);

                _logger.LogInformation(
                    "Evento publicado {EventType} de la orden {OrderId}",
                    domainEvent.GetType().Name,
                    order.OrderId);
            }
        }
    }

    private static DomainEvent? Deserialize(string typeName, string content)
    {
        var type = Type.GetType(typeName);
        if (type is null)
        {
            return null;
        }

        return JsonSerializer.Deserialize(content, type) as DomainEvent;
    }
}
