using System.ComponentModel.DataAnnotations;

namespace OrderOrchestration.Api.Bff.Models
{
    /// <summary>
    /// Petición REST para enviar una nueva orden. El BFF la traduce a la llamada gRPC <c>SubmitOrder</c>.
    /// </summary>
    public class SubmitOrderRequestDto
    {
        /// <summary>Opcional: si viene vacío, el backend genera el identificador.</summary>
        public string? OrderId { get; set; }

        [Required(ErrorMessage = "UserId es requerido.")]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [MinLength(1, ErrorMessage = "La orden debe contener al menos un item.")]
        public List<OrderItemDto> Items { get; set; } = new();

        [Range(0.01, 1000000.00, ErrorMessage = "TotalAmount debe estar entre 0.01 y 1,000,000.00.")]
        public decimal TotalAmount { get; set; }

        [Required(ErrorMessage = "PaymentToken es requerido.")]
        public string PaymentToken { get; set; } = string.Empty;
    }
}
