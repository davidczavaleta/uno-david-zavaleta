namespace OrderOrchestration.Api.Bff.Models
{
    /// <summary>
    /// Actualización de estado que el BFF envía a los clientes vía SignalR.
    /// </summary>
    public record OrderStatusDto(string OrderId, string Status, string Timestamp);
}
