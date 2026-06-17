namespace OrderOrchestration.Api.Bff.Models
{
    /// <summary>
    /// Orden pendiente de revisión manual, expuesta al panel admin.
    /// </summary>
    public record PendingReviewDto(string OrderId, string UserId, decimal TotalAmount, string CreatedAt);
}
