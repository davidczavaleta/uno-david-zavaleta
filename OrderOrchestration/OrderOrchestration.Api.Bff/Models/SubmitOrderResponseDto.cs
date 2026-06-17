namespace OrderOrchestration.Api.Bff.Models
{
    /// <summary>
    /// Respuesta REST tras enviar una orden.
    /// </summary>
    public record SubmitOrderResponseDto(string OrderId, string Status);
}
