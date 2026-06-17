using System.ComponentModel.DataAnnotations;

namespace OrderOrchestration.Api.Bff.Models
{
    /// <summary>
    /// Item de una orden recibido por el BFF a través de REST.
    /// </summary>
    public class OrderItemDto
    {
        [Required]
        public string ProductId { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor a cero.")]
        public int Quantity { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "El precio unitario debe ser mayor a cero.")]
        public decimal UnitPrice { get; set; }
    }
}
